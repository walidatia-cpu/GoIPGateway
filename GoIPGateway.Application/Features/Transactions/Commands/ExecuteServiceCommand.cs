using System.Text.Json;
using FluentValidation;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Factories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Features.Transactions.Commands;

public record ExecuteServiceCommand(
    string ServiceCode,
    string? ExternalRef,
    int? SimCardId,
    OperatorType? Operator,
    Dictionary<string, string> Parameters,
    string? CallbackUrl = null,
    RequestSource Source = RequestSource.API
) : IRequest<Result<TransactionResultDto>>;

public class ExecuteServiceCommandValidator : AbstractValidator<ExecuteServiceCommand>
{
    public ExecuteServiceCommandValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithMessage("كود الخدمة مطلوب / Service code is required.")
            .MaximumLength(50).WithMessage("كود الخدمة لا يجب أن يتجاوز 50 حرفاً.");

        RuleFor(x => x.ExternalRef)
            .MaximumLength(100).WithMessage("المرجع الخارجي لا يجب أن يتجاوز 100 حرف.");

        RuleFor(x => x.CallbackUrl)
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("رابط الإشعار (Callback URL) غير صالح.");
    }
}

public class ExecuteServiceCommandHandler : IRequestHandler<ExecuteServiceCommand, Result<TransactionResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IServiceExecutorFactory _executorFactory;
    private readonly ISignalRNotifier _notifier;

    public ExecuteServiceCommandHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        IServiceExecutorFactory executorFactory,
        ISignalRNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _executorFactory = executorFactory;
        _notifier = notifier;
    }

    public async Task<Result<TransactionResultDto>> Handle(ExecuteServiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue && !_currentUser.IsSuperAdmin)
        {
            return Result.Failure<TransactionResultDto>("Auth.UnauthorizedTenant", "لم يتم تحديد الشركة المستأجرة.");
        }

        // 1. Verify Tenant
        Tenant? tenant = null;
        if (tenantId.HasValue)
        {
            tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value && !t.IsDelete, cancellationToken);
            if (tenant == null)
                return Result.Failure<TransactionResultDto>("Tenant.NotFound", "الشركة غير موجودة.");

            if (!tenant.IsActive || tenant.IsBlocked)
                return Result.Failure<TransactionResultDto>("Tenant.Blocked", $"حساب الشركة متوقف أو محظور: {tenant.BlockReason}");
        }

        // 2. Verify Service Definition
        var service = await _context.ServiceDefinitions
            .Include(s => s.Parameters)
            .Include(s => s.UssdSteps.OrderBy(st => st.StepOrder))
            .FirstOrDefaultAsync(s => s.ServiceCode == request.ServiceCode && s.IsActive && !s.IsDelete, cancellationToken);

        if (service == null)
            return Result.Failure<TransactionResultDto>("Service.NotFound", $"الخدمة '{request.ServiceCode}' غير موجودة أو غير مفعلة.");

        // Check Tenant Subscription if applicable
        TenantService? tenantService = null;
        if (tenant != null && !service.IsSystemService)
        {
            tenantService = await _context.TenantServices
                .FirstOrDefaultAsync(ts => ts.TenantId == tenant.Id && ts.ServiceDefinitionId == service.Id && ts.IsActive, cancellationToken);

            if (tenantService == null)
                return Result.Failure<TransactionResultDto>("Service.NotSubscribed", $"الشركة غير مشتركة في الخدمة '{service.NameAr}'.");
        }

        // Check Token Scoped Allowed Services
        if (!string.IsNullOrWhiteSpace(_currentUser.AllowedServices))
        {
            try
            {
                bool isAllowed = false;
                bool parsed = false;

                try
                {
                    var idList = JsonSerializer.Deserialize<List<int>>(_currentUser.AllowedServices);
                    if (idList != null && idList.Any())
                    {
                        isAllowed = idList.Contains(service.Id);
                        parsed = true;
                    }
                }
                catch { }

                if (!parsed)
                {
                    try
                    {
                        var codeList = JsonSerializer.Deserialize<List<string>>(_currentUser.AllowedServices);
                        if (codeList != null && codeList.Any())
                        {
                            isAllowed = codeList.Contains(service.ServiceCode, StringComparer.OrdinalIgnoreCase);
                            parsed = true;
                        }
                    }
                    catch { }
                }

                if (parsed && !isAllowed)
                {
                    return Result.Failure<TransactionResultDto>("Token.ServiceNotAllowed",
                        $"مفتاح الربط (Token) غير مصرح له بتنفيذ الخدمة '{service.NameAr}' ({service.ServiceCode}). هذا التوكن مقتصر على خدمات معينة فقط.");
                }
            }
            catch
            {
                // ignore unexpected parsing issues
            }
        }

        // 3. Dynamic Parameter Validation
        foreach (var paramDef in service.Parameters.Where(p => p.IsActive))
        {
            if (paramDef.IsRequired && (!request.Parameters.TryGetValue(paramDef.ParameterKey, out var val) || string.IsNullOrWhiteSpace(val)))
            {
                return Result.Failure<TransactionResultDto>("Validation.MissingParam", $"البارامتر '{paramDef.DisplayNameAr ?? paramDef.ParameterName}' مطلوب.");
            }
        }

        // Extract Amount if passed
        decimal? transactionAmount = null;
        if (request.Parameters.TryGetValue("amount", out var amountStr) && decimal.TryParse(amountStr, out var amt))
        {
            transactionAmount = amt;
        }

        // 4. Resolve SIM Card
        SimCard? selectedSim = null;
        if (request.SimCardId.HasValue)
        {
            // Verify if this service has dedicated assigned SIM cards
            if (tenantService != null && tenantService.AssignedSimCardIds.Any() && !tenantService.AssignedSimCardIds.Contains(request.SimCardId.Value))
            {
                return Result.Failure<TransactionResultDto>("Routing.SimNotAllowedForService",
                    $"الشريحة المحددة #{request.SimCardId.Value} غير مخصصة لتنفيذ خدمة '{service.NameAr}' في إعدادات توجيه الشركة.");
            }

            selectedSim = await _context.SimCards
                .Include(s => s.DevicePort)
                    .ThenInclude(p => p.Device)
                .FirstOrDefaultAsync(s => s.Id == request.SimCardId.Value && s.IsActive && !s.IsDelete, cancellationToken);

            if (selectedSim == null)
                return Result.Failure<TransactionResultDto>("SimCard.NotFound", "الشريحة المحددة غير موجودة.");
        }
        else
        {
            // Auto SIM Selection Algorithm
            var simQuery = _context.SimCards
                .Include(s => s.DevicePort)
                    .ThenInclude(p => p.Device)
                .Where(s => s.IsActive && !s.IsDelete && !s.IsBlocked && s.DevicePort.IsActive && !s.DevicePort.IsBusy && s.DevicePort.Device.IsActive && s.DevicePort.Device.IsOnline);

            // Filter by assigned SIMs in service routing if configured
            if (tenantService != null && tenantService.AssignedSimCardIds.Any())
            {
                simQuery = simQuery.Where(s => tenantService.AssignedSimCardIds.Contains(s.Id));
            }

            // Filter by requested operator or service operator
            var targetOp = request.Operator ?? service.Operator;
            if (targetOp.HasValue)
            {
                simQuery = simQuery.Where(s => s.Operator == targetOp.Value);
            }

            // Filter by tenant-assigned devices if tenant exists
            if (tenant != null)
            {
                var assignedDeviceIds = await _context.TenantDevices
                    .Where(td => td.TenantId == tenant.Id && td.IsActive)
                    .Select(td => td.DeviceId)
                    .ToListAsync(cancellationToken);

                if (assignedDeviceIds.Any())
                {
                    simQuery = simQuery.Where(s => assignedDeviceIds.Contains(s.DevicePort.DeviceId));
                }
            }

            // Filter by balance if amount is specified
            if (transactionAmount.HasValue && transactionAmount.Value > 0 && service.BalanceActionType == BalanceActionType.Decrement)
            {
                simQuery = simQuery.Where(s => s.CurrentBalance >= transactionAmount.Value);
            }

            // Order by least consumed operations today
            selectedSim = await simQuery
                .OrderBy(s => s.DailyConsumed)
                .FirstOrDefaultAsync(cancellationToken);

            if (selectedSim == null)
            {
                return Result.Failure<TransactionResultDto>("SimCard.Unavailable", "لا توجد شريحة متاحة حالياً لتنفيذ العملية (الشرايح مشغولة أو الرصيد غير كافي).");
            }
        }

        // 5. Create Transaction Record
        var cost = tenantService?.CustomPrice ?? service.BaseCost;
        var txRef = $"GW-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var transaction = new TenantTransaction
        {
            TransactionRef = txRef,
            ExternalRef = request.ExternalRef,
            TenantId = tenant?.Id ?? 1,
            ServiceDefinitionId = service.Id,
            SimCardId = selectedSim.Id,
            DevicePortId = selectedSim.DevicePortId,
            Parameters = JsonSerializer.Serialize(request.Parameters),
            Amount = transactionAmount,
            Status = TransactionStatus.Pending,
            ServiceCost = service.BaseCost,
            TenantCharge = cost,
            RequestedAt = DateTime.UtcNow,
            RequestedById = _currentUser.UserId,
            RequestSource = request.Source,
            CallbackUrl = request.CallbackUrl ?? tenantService?.WebhookUrl
        };

        _context.TenantTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        // Notify SignalR of new transaction
        await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);

        // 6. Execute via Executor Factory
        var executor = _executorFactory.GetExecutor(service.ServiceType);
        var execResult = await executor.ExecuteAsync(transaction, cancellationToken);

        return execResult;
    }
}
