using System.Text.Json;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Factories;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.Transactions;

public class TransactionCommandService : ITransactionCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IServiceExecutorFactory _executorFactory;
    private readonly ISignalRNotifier _notifier;

    public TransactionCommandService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        IServiceExecutorFactory executorFactory,
        ISignalRNotifier notifier)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _executorFactory = executorFactory;
        _notifier = notifier;
    }

    public async Task<Result<TransactionResultDto>> ExecuteServiceAsync(ExecuteServiceCommand command, CancellationToken cancellationToken = default)
    {
        // يتم التحقق الاستباقي من هوية وصلاحية حساب الشركة عبر TenantVerificationBehavior في بايبلاين MediatR
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId ?? 1;
        var tenant = await _unitOfWork.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDelete, cancellationToken);

        // 2. Verify Service Definition
        var service = await _unitOfWork.Services.AsQueryable()
            .Include(s => s.Parameters)
            .Include(s => s.UssdSteps.OrderBy(st => st.StepOrder))
            .FirstOrDefaultAsync(s => s.ServiceCode == command.ServiceCode && s.IsActive && !s.IsDelete, cancellationToken);

        if (service == null)
            return Result.Failure<TransactionResultDto>("Service.NotFound", $"الخدمة '{command.ServiceCode}' غير موجودة أو غير مفعلة.");

        // Check Tenant Subscription if applicable
        TenantService? tenantService = null;
        if (tenant != null && !service.IsSystemService)
        {
            tenantService = await _unitOfWork.TenantServices
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

        // 3. البارامترات الموحدة للخدمة
        var parameters = new Dictionary<string, string>(command.Parameters ?? new(), StringComparer.OrdinalIgnoreCase);
        // Validate against ServiceDefinition Parameters
        foreach (var paramDef in service.Parameters.Where(p => p.IsActive))
        {
            var key = paramDef.ParameterKey?.Trim() ?? "";
            if (string.IsNullOrEmpty(key)) continue;

            // Auto-fill default value if missing or whitespace
            if (!parameters.TryGetValue(key, out var val) || string.IsNullOrWhiteSpace(val))
            {
                if (!string.IsNullOrWhiteSpace(paramDef.DefaultValue))
                {
                    val = paramDef.DefaultValue.Trim();
                    parameters[key] = val;
                }
            }

            if (paramDef.IsRequired && string.IsNullOrWhiteSpace(val))
            {
                // Defer merchantCode check to see if provided by the assigned GoIP device
                if (key.Equals("merchantCode", StringComparison.OrdinalIgnoreCase) || key.Equals("merchant", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var label = !string.IsNullOrWhiteSpace(paramDef.DisplayNameAr)
                    ? paramDef.DisplayNameAr
                    : (!string.IsNullOrWhiteSpace(paramDef.ParameterName) ? paramDef.ParameterName : key);
                return Result.Failure<TransactionResultDto>("Validation.MissingParam", $"البارامتر '{label}' ({key}) مطلوب لإتمام العملية.");
            }

            // Data type validation
            if (!string.IsNullOrWhiteSpace(val))
            {
                if (paramDef.DataType == ParameterDataType.Decimal || paramDef.DataType == ParameterDataType.Number)
                {
                    if (!decimal.TryParse(val, out _))
                    {
                        var label = paramDef.DisplayNameAr ?? key;
                        return Result.Failure<TransactionResultDto>("Validation.InvalidNumber", $"قيمة الحقل '{label}' يجب أن تكون رقماً صالحاً.");
                    }
                }
                else if (paramDef.DataType == ParameterDataType.Phone)
                {
                    var cleanPhone = System.Text.RegularExpressions.Regex.Replace(val, @"[^\d]", "");
                    if (cleanPhone.Length < 9)
                    {
                        var label = paramDef.DisplayNameAr ?? key;
                        return Result.Failure<TransactionResultDto>("Validation.InvalidPhone", $"رقم الهاتف في الحقل '{label}' غير صالح.");
                    }
                }
            }
        }

        // Extract Amount if passed
        decimal? transactionAmount = null;
        if (parameters.TryGetValue("amount", out var amountStr) && decimal.TryParse(amountStr, out var amt))
        {
            transactionAmount = amt;
        }

        // 4. Resolve SIM Card
        SimCard? selectedSim = null;
        if (command.SimCardId.HasValue)
        {
            // Verify if this service has dedicated assigned SIM cards
            if (tenantService != null && tenantService.AssignedSimCardIds.Any() && !tenantService.AssignedSimCardIds.Contains(command.SimCardId.Value))
            {
                return Result.Failure<TransactionResultDto>("Routing.SimNotAllowedForService",
                    $"الشريحة المحددة #{command.SimCardId.Value} غير مخصصة لتنفيذ خدمة '{service.NameAr}' في إعدادات توجيه الشركة.");
            }

            selectedSim = await _unitOfWork.SimCards.AsQueryable()
                .Include(s => s.DevicePort)
                    .ThenInclude(p => p.Device)
                .FirstOrDefaultAsync(s => s.Id == command.SimCardId.Value && s.IsActive && !s.IsDelete, cancellationToken);

            if (selectedSim == null)
                return Result.Failure<TransactionResultDto>("SimCard.NotFound", "الشريحة المحددة غير موجودة.");
        }
        else
        {
            // Auto SIM Selection Algorithm
            var simQuery = _unitOfWork.SimCards.AsQueryable()
                .Include(s => s.DevicePort)
                    .ThenInclude(p => p.Device)
                .Where(s => s.IsActive && !s.IsDelete && !s.IsBlocked && s.DevicePort.IsActive && !s.DevicePort.IsBusy && s.DevicePort.Device.IsActive && s.DevicePort.Device.IsOnline);

            // Filter by assigned SIMs in service routing if configured
            if (tenantService != null && tenantService.AssignedSimCardIds.Any())
            {
                simQuery = simQuery.Where(s => tenantService.AssignedSimCardIds.Contains(s.Id));
            }

            // Filter by requested operator or service operator
            var targetOp = command.Operator ?? service.Operator;
            if (targetOp.HasValue)
            {
                simQuery = simQuery.Where(s => s.Operator == targetOp.Value);
            }

            // Filter by tenant-assigned devices if tenant exists
            if (tenant != null)
            {
                var tenantDevicesRepo = _unitOfWork.Repository<TenantDevice, int>();
                var assignedDeviceIds = await tenantDevicesRepo.AsQueryable()
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

        // Auto-fill MerchantCode from Device settings if not sent in request parameters
        if (!parameters.ContainsKey("merchantCode") && !string.IsNullOrWhiteSpace(selectedSim.DevicePort?.Device?.MerchantCode))
        {
            parameters["merchantCode"] = selectedSim.DevicePort.Device.MerchantCode.Trim();
        }

        // Validate if merchantCode was required by the service and still missing
        var requiresMerchant = service.Parameters.Any(p => p.IsActive && p.IsRequired &&
            (p.ParameterKey.Equals("merchantCode", StringComparison.OrdinalIgnoreCase) || p.ParameterKey.Equals("merchant", StringComparison.OrdinalIgnoreCase)));
        if (requiresMerchant && !parameters.ContainsKey("merchantCode"))
        {
            return Result.Failure<TransactionResultDto>("Validation.MissingMerchantCode",
                "كود التاجر (MerchantCode) مطلوب لإتمام هذه الخدمة، ولم يتم إرساله بالطلب أو ضبطه في إعدادات جهاز GoIP.");
        }

        // 5. Create Transaction Record
        var cost = tenantService?.CustomPrice ?? service.BaseCost;
        var txRef = $"GW-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var transaction = new TenantTransaction
        {
            TransactionRef = txRef,
            ExternalRef = command.ExternalRef,
            TenantId = tenant?.Id ?? 1,
            ServiceDefinitionId = service.Id,
            ServiceDefinition = service,
            SimCardId = selectedSim.Id,
            SimCard = selectedSim,
            DevicePortId = selectedSim.DevicePortId,
            DevicePort = selectedSim.DevicePort,
            Parameters = JsonSerializer.Serialize(parameters),
            Amount = transactionAmount,
            Status = TransactionStatus.Pending,
            ServiceCost = service.BaseCost,
            TenantCharge = cost,
            RequestedAt = DateTime.UtcNow,
            RequestedById = _currentUser.UserId,
            RequestSource = command.Source,
            CallbackUrl = command.CallbackUrl ?? tenantService?.WebhookUrl
        };

        await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify SignalR of new transaction
        await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);

        // 6. Execute via Executor Factory
        var executor = _executorFactory.GetExecutor(service.ServiceType);
        var execResult = await executor.ExecuteAsync(transaction, cancellationToken);

        return execResult;
    }
}
