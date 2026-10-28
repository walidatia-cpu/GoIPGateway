using System.Diagnostics;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Factories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Services.Executors;

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
                SimCardId = sim.Id,
                SimPhoneNumber = sim.PhoneNumber,
                PortNumber = sim.DevicePort?.PortNumber,
                Operator = sim.Operator,
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
