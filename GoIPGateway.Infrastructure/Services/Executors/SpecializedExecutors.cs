using System.Diagnostics;
using System.Text.Json;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Factories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.Services.Executors;

public class BalanceInquiryExecutor : IServiceExecutor
{
    private readonly IAppDbContext _context;
    private readonly IGoipCommunicationClient _goipClient;
    private readonly IPortLockManager _lockManager;
    private readonly IOperatorStrategyFactory _operatorFactory;
    private readonly ISignalRNotifier _notifier;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ILogger<BalanceInquiryExecutor> _logger;

    public BalanceInquiryExecutor(
        IAppDbContext context,
        IGoipCommunicationClient goipClient,
        IPortLockManager lockManager,
        IOperatorStrategyFactory operatorFactory,
        ISignalRNotifier notifier,
        IWebhookDispatcher webhookDispatcher,
        ILogger<BalanceInquiryExecutor> logger)
    {
        _context = context;
        _goipClient = goipClient;
        _lockManager = lockManager;
        _operatorFactory = operatorFactory;
        _notifier = notifier;
        _webhookDispatcher = webhookDispatcher;
        _logger = logger;
    }

    public bool CanHandle(ServiceType serviceType) => serviceType == ServiceType.BalanceInquiry;

    public async Task<Result<TransactionResultDto>> ExecuteAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (!transaction.SimCardId.HasValue || !transaction.DevicePortId.HasValue)
            return Result.Failure<TransactionResultDto>("Sim.NotFound", "الشريحة أو البورت غير محدد.");

        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .FirstOrDefaultAsync(s => s.Id == transaction.SimCardId.Value, cancellationToken);

        if (sim == null || sim.DevicePort == null || sim.DevicePort.Device == null)
            return Result.Failure<TransactionResultDto>("Device.NotFound", "جهاز GoIP أو البورت غير متاح.");

        var locked = await _lockManager.TryAcquireLockAsync(sim.DevicePortId, TimeSpan.FromSeconds(15), cancellationToken);
        if (!locked)
            return Result.Failure<TransactionResultDto>("Port.Busy", "البورت مشغول بعملية أخرى.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var opStrategy = _operatorFactory.GetStrategy(sim.Operator);
            var commandTemplate = !string.IsNullOrWhiteSpace(sim.WalletPassword) 
                ? opStrategy.DefaultWalletBalanceInquiryCommand 
                : opStrategy.DefaultBalanceInquiryCommand;

            var command = commandTemplate
                .Replace("{Password}", sim.WalletPassword ?? "")
                .Replace("Password", sim.WalletPassword ?? "");

            transaction.Status = TransactionStatus.Processing;
            transaction.ProcessingStartedAt = DateTime.UtcNow;
            transaction.SimBalanceBefore = sim.CurrentBalance;
            await _context.SaveChangesAsync(cancellationToken);

            var rawResponse = await _goipClient.SendUssdCommandAsync(
                sim.DevicePort.Device,
                sim.DevicePort.PortNumber,
                command,
                30,
                cancellationToken);

            stopwatch.Stop();
            var extractedBalance = opStrategy.ExtractBalance(rawResponse);

            var isSuccess = extractedBalance.HasValue || !string.IsNullOrWhiteSpace(rawResponse);

            var log = new TransactionLog
            {
                TransactionId = transaction.Id,
                StepOrder = 1,
                StepName = "استعلام الرصيد",
                UssdCommand = command,
                UssdResponse = rawResponse,
                IsSuccess = isSuccess,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };
            _context.TransactionLogs.Add(log);

            transaction.Status = isSuccess ? TransactionStatus.Completed : TransactionStatus.Failed;
            transaction.RawResponse = rawResponse;
            transaction.CompletedAt = DateTime.UtcNow;

            if (extractedBalance.HasValue)
            {
                var prev = sim.CurrentBalance;
                sim.PreviousBalance = prev;
                sim.CurrentBalance = extractedBalance.Value;
                sim.LastBalanceCheck = DateTime.UtcNow;
                transaction.SimBalanceAfter = sim.CurrentBalance;

                _context.SimBalanceHistories.Add(new SimBalanceHistory
                {
                    SimCardId = sim.Id,
                    BalanceBefore = prev,
                    BalanceAfter = sim.CurrentBalance,
                    ChangeAmount = sim.CurrentBalance - prev,
                    ChangeType = BalanceActionType.None,
                    TransactionId = transaction.Id,
                    Notes = "Balance inquiry USSD",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);
            await _notifier.NotifySimBalanceChangedAsync(sim, cancellationToken);
            await _webhookDispatcher.DispatchTransactionWebhookAsync(transaction, cancellationToken);

            return Result.Success(new TransactionResultDto
            {
                TransactionId = transaction.Id,
                TransactionRef = transaction.TransactionRef,
                ExternalRef = transaction.ExternalRef,
                Status = transaction.Status,
                Message = extractedBalance.HasValue ? $"الرصيد المستعلم: {extractedBalance.Value:N2} ج.م" : rawResponse,
                RawResponse = rawResponse,
                SimBalanceBefore = transaction.SimBalanceBefore,
                SimBalanceAfter = transaction.SimBalanceAfter,
                CompletedAt = transaction.CompletedAt.Value,
                Logs = new List<TransactionLogDto>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = log.StepName,
                        Command = log.UssdCommand,
                        Response = log.UssdResponse,
                        IsSuccess = log.IsSuccess,
                        DurationMs = log.DurationMs
                    }
                }
            });
        }
        finally
        {
            await _lockManager.ReleaseLockAsync(sim.DevicePortId);
        }
    }
}

public class SimNumberInquiryExecutor : IServiceExecutor
{
    private readonly IAppDbContext _context;
    private readonly IGoipCommunicationClient _goipClient;
    private readonly IPortLockManager _lockManager;
    private readonly IOperatorStrategyFactory _operatorFactory;
    private readonly ISignalRNotifier _notifier;

    public SimNumberInquiryExecutor(
        IAppDbContext context,
        IGoipCommunicationClient goipClient,
        IPortLockManager lockManager,
        IOperatorStrategyFactory operatorFactory,
        ISignalRNotifier notifier)
    {
        _context = context;
        _goipClient = goipClient;
        _lockManager = lockManager;
        _operatorFactory = operatorFactory;
        _notifier = notifier;
    }

    public bool CanHandle(ServiceType serviceType) => serviceType == ServiceType.SimNumberInquiry;

    public async Task<Result<TransactionResultDto>> ExecuteAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (!transaction.SimCardId.HasValue)
            return Result.Failure<TransactionResultDto>("Sim.NotFound", "الشريحة غير محددة.");

        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .FirstOrDefaultAsync(s => s.Id == transaction.SimCardId.Value, cancellationToken);

        if (sim == null || sim.DevicePort == null || sim.DevicePort.Device == null)
            return Result.Failure<TransactionResultDto>("Device.NotFound", "الجهاز أو البورت غير متوفر.");

        var locked = await _lockManager.TryAcquireLockAsync(sim.DevicePortId, TimeSpan.FromSeconds(15), cancellationToken);
        if (!locked)
            return Result.Failure<TransactionResultDto>("Port.Busy", "البورت مشغول.");

        var sw = Stopwatch.StartNew();
        try
        {
            var opStrategy = _operatorFactory.GetStrategy(sim.Operator);
            var command = opStrategy.DefaultNumberInquiryCommand;

            transaction.Status = TransactionStatus.Processing;
            transaction.ProcessingStartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var rawResponse = await _goipClient.SendUssdCommandAsync(
                sim.DevicePort.Device,
                sim.DevicePort.PortNumber,
                command,
                30,
                cancellationToken);

            sw.Stop();
            var extractedPhone = opStrategy.ExtractPhoneNumber(rawResponse);

            if (!string.IsNullOrWhiteSpace(extractedPhone))
            {
                sim.PhoneNumber = extractedPhone;
                sim.SimNumber = extractedPhone;
            }

            transaction.Status = !string.IsNullOrEmpty(extractedPhone) ? TransactionStatus.Completed : TransactionStatus.Failed;
            transaction.RawResponse = rawResponse;
            transaction.CompletedAt = DateTime.UtcNow;

            var log = new TransactionLog
            {
                TransactionId = transaction.Id,
                StepOrder = 1,
                StepName = "استعلام رقم الشريحة",
                UssdCommand = command,
                UssdResponse = rawResponse,
                IsSuccess = !string.IsNullOrEmpty(extractedPhone),
                DurationMs = (int)sw.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };
            _context.TransactionLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);

            return Result.Success(new TransactionResultDto
            {
                TransactionId = transaction.Id,
                TransactionRef = transaction.TransactionRef,
                ExternalRef = transaction.ExternalRef,
                Status = transaction.Status,
                Message = extractedPhone != null ? $"تم استخراج الرقم: {extractedPhone}" : rawResponse,
                RawResponse = rawResponse,
                CompletedAt = transaction.CompletedAt.Value,
                Logs = new List<TransactionLogDto>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = log.StepName,
                        Command = log.UssdCommand,
                        Response = log.UssdResponse,
                        IsSuccess = log.IsSuccess,
                        DurationMs = log.DurationMs
                    }
                }
            });
        }
        finally
        {
            await _lockManager.ReleaseLockAsync(sim.DevicePortId);
        }
    }
}

public class QrPaymentExecutor : IServiceExecutor
{
    private readonly UssdMultiStepExecutor _ussdExecutor;

    public QrPaymentExecutor(UssdMultiStepExecutor ussdExecutor)
    {
        _ussdExecutor = ussdExecutor;
    }

    public bool CanHandle(ServiceType serviceType) => serviceType == ServiceType.QRPayment;

    public Task<Result<TransactionResultDto>> ExecuteAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        return _ussdExecutor.ExecuteAsync(transaction, cancellationToken);
    }
}
