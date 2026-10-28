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

public class UssdMultiStepExecutor : IServiceExecutor
{
    private readonly IAppDbContext _context;
    private readonly IGoipCommunicationClient _goipClient;
    private readonly IPortLockManager _lockManager;
    private readonly ISignalRNotifier _notifier;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ILogger<UssdMultiStepExecutor> _logger;

    public UssdMultiStepExecutor(
        IAppDbContext context,
        IGoipCommunicationClient goipClient,
        IPortLockManager lockManager,
        ISignalRNotifier notifier,
        IWebhookDispatcher webhookDispatcher,
        ILogger<UssdMultiStepExecutor> logger)
    {
        _context = context;
        _goipClient = goipClient;
        _lockManager = lockManager;
        _notifier = notifier;
        _webhookDispatcher = webhookDispatcher;
        _logger = logger;
    }

    public bool CanHandle(ServiceType serviceType) =>
        serviceType is ServiceType.USSD or ServiceType.Transfer or ServiceType.Custom or ServiceType.Settlement;

    public async Task<Result<TransactionResultDto>> ExecuteAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (!transaction.DevicePortId.HasValue || !transaction.SimCardId.HasValue)
        {
            return FailTransaction(transaction, "MissingPortOrSim", "لم يتم ربط العملية ببورت أو شريحة صالحة.");
        }

        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .FirstOrDefaultAsync(s => s.Id == transaction.SimCardId.Value, cancellationToken);

        if (sim == null || sim.DevicePort == null || sim.DevicePort.Device == null)
        {
            return FailTransaction(transaction, "DeviceUnavailable", "جهاز GoIP أو الشريحة غير متوفرين.");
        }

        var service = await _context.ServiceDefinitions
            .Include(s => s.UssdSteps.OrderBy(st => st.StepOrder))
            .FirstOrDefaultAsync(s => s.Id == transaction.ServiceDefinitionId, cancellationToken);

        if (service == null || !service.UssdSteps.Any())
        {
            return FailTransaction(transaction, "NoStepsConfigured", "لا توجد خطوات USSD معرفة لهذه الخدمة.");
        }

        // 1. Acquire Port Lock
        var locked = await _lockManager.TryAcquireLockAsync(sim.DevicePortId, TimeSpan.FromSeconds(15), cancellationToken);
        if (!locked)
        {
            return FailTransaction(transaction, "PortLocked", "البورت مشغول حالياً بعملية أخرى. يرجى المحاولة بعد قليل.");
        }

        var stopwatch = new Stopwatch();
        var logs = new List<TransactionLog>();
        var parameters = string.IsNullOrEmpty(transaction.Parameters)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(transaction.Parameters) ?? new();

        transaction.Status = TransactionStatus.Processing;
        transaction.ProcessingStartedAt = DateTime.UtcNow;
        transaction.SimBalanceBefore = sim.CurrentBalance;
        transaction.BankBalanceBefore = sim.BankBalance;
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);

        try
        {
            string? lastResponse = null;
            string? finalSuccessClientMessage = null;

            foreach (var step in service.UssdSteps.Where(s => s.IsActive))
            {
                stopwatch.Restart();

                // Evaluate placeholders
                var command = EvaluatePlaceholders(step.UssdCommand, sim, parameters, transaction.Amount);

                _logger.LogInformation("Tx {TxRef} Step {Order}: Executing USSD: {Command}", transaction.TransactionRef, step.StepOrder, command);

                var response = await _goipClient.SendUssdCommandAsync(
                    sim.DevicePort.Device,
                    sim.DevicePort.PortNumber,
                    command,
                    step.TimeoutSeconds,
                    cancellationToken);

                stopwatch.Stop();
                lastResponse = response;

                var isSuccess = true;
                string? stepError = null;
                string? clientSafeMessage = null;

                // 1. Evaluate Failure Rules (Pattern -> Safe Client Message Mapping)
                var failureRules = step.ParsedFailureRules;
                var matchedFailure = failureRules.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Pattern) && response.Contains(f.Pattern, StringComparison.OrdinalIgnoreCase));

                if (matchedFailure != null)
                {
                    isSuccess = false;
                    stepError = $"فشلت الخطوة: الاستجابة تطابق نمط الفشل ({matchedFailure.Pattern})";
                    clientSafeMessage = !string.IsNullOrWhiteSpace(matchedFailure.ClientMessage)
                        ? matchedFailure.ClientMessage
                        : $"فشلت العملية: {matchedFailure.Pattern}";
                }
                else
                {
                    // 2. Evaluate Success Rules (Pattern -> Custom Client Message Mapping)
                    var successRules = step.ParsedSuccessRules;
                    var noWaitRule = successRules.FirstOrDefault(s => s.Pattern.Equals("NoNeedToWait", StringComparison.OrdinalIgnoreCase));
                    if (noWaitRule != null)
                    {
                        isSuccess = true;
                        if (!string.IsNullOrWhiteSpace(noWaitRule.ClientMessage))
                        {
                            clientSafeMessage = noWaitRule.ClientMessage;
                            finalSuccessClientMessage = clientSafeMessage;
                        }
                    }
                    else if (successRules.Any())
                    {
                        var matchedSuccess = successRules.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Pattern) && response.Contains(s.Pattern, StringComparison.OrdinalIgnoreCase));
                        if (matchedSuccess != null)
                        {
                            isSuccess = true;
                            if (!string.IsNullOrWhiteSpace(matchedSuccess.ClientMessage))
                            {
                                clientSafeMessage = matchedSuccess.ClientMessage;
                                finalSuccessClientMessage = clientSafeMessage;
                            }
                        }
                        else
                        {
                            isSuccess = false;
                            stepError = "فشلت الخطوة: استجابة الـ USSD لا تطابق أياً من عبارات النجاح المحددة للمسار.";
                            clientSafeMessage = "تعذر تأكيد نجاح العملية من الشبكة، يرجى التحقق من الرصيد أو المحاولة لاحقاً.";
                        }
                    }
                }

                var log = new TransactionLog
                {
                    TransactionId = transaction.Id,
                    StepOrder = step.StepOrder,
                    StepName = step.StepTitle ?? $"Step {step.StepOrder}",
                    UssdCommand = command,
                    UssdResponse = response,
                    IsSuccess = isSuccess,
                    ErrorMessage = stepError,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    CreatedAt = DateTime.UtcNow
                };

                logs.Add(log);
                _context.TransactionLogs.Add(log);
                await _context.SaveChangesAsync(cancellationToken);

                if (!isSuccess)
                {
                    var finalErrorMessage = clientSafeMessage ?? stepError ?? "فشلت خطوة أثناء التنفيذ.";
                    transaction.Status = TransactionStatus.Failed;
                    transaction.ErrorMessage = finalErrorMessage;
                    transaction.ErrorCode = "StepExecutionFailed";
                    transaction.RawResponse = response;
                    transaction.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);
                    await _webhookDispatcher.DispatchTransactionWebhookAsync(transaction, cancellationToken);

                    return Result.Failure<TransactionResultDto>("Tx.StepFailed", finalErrorMessage);
                }

                if (step.DelayAfterMs > 0)
                {
                    await Task.Delay(step.DelayAfterMs, cancellationToken);
                }
            }

            // All steps succeeded
            transaction.Status = TransactionStatus.Completed;
            transaction.RawResponse = lastResponse;
            transaction.CompletedAt = DateTime.UtcNow;

            // Handle Balance updates
            if (service.BalanceActionType == BalanceActionType.Decrement && transaction.Amount.HasValue)
            {
                sim.CurrentBalance -= transaction.Amount.Value;
                sim.DailyConsumed += transaction.Amount.Value;
                sim.MonthlyConsumed += transaction.Amount.Value;
                transaction.SimBalanceAfter = sim.CurrentBalance;

                _context.SimBalanceHistories.Add(new SimBalanceHistory
                {
                    SimCardId = sim.Id,
                    BalanceBefore = transaction.SimBalanceBefore ?? 0,
                    BalanceAfter = sim.CurrentBalance,
                    ChangeAmount = -transaction.Amount.Value,
                    ChangeType = BalanceActionType.Decrement,
                    TransactionId = transaction.Id,
                    Notes = $"Transaction {transaction.TransactionRef} execution",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);
            await _notifier.NotifySimBalanceChangedAsync(sim, cancellationToken);
            await _webhookDispatcher.DispatchTransactionWebhookAsync(transaction, cancellationToken);

            var resultDto = new TransactionResultDto
            {
                TransactionId = transaction.Id,
                TransactionRef = transaction.TransactionRef,
                ExternalRef = transaction.ExternalRef,
                Status = TransactionStatus.Completed,
                Message = finalSuccessClientMessage ?? "تم تنفيذ جميع خطوات الخدمة بنجاح",
                RawResponse = lastResponse,
                SimBalanceBefore = transaction.SimBalanceBefore,
                SimBalanceAfter = transaction.SimBalanceAfter,
                SimCardId = sim.Id,
                SimPhoneNumber = sim.PhoneNumber,
                PortNumber = sim.DevicePort?.PortNumber,
                Operator = sim.Operator,
                CompletedAt = transaction.CompletedAt.Value,
                Logs = logs.Select(l => new TransactionLogDto
                {
                    StepOrder = l.StepOrder,
                    StepName = l.StepName,
                    Command = l.UssdCommand,
                    Response = l.UssdResponse,
                    IsSuccess = l.IsSuccess,
                    DurationMs = l.DurationMs
                }).ToList()
            };

            return Result.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing Transaction {TxRef}", transaction.TransactionRef);
            return FailTransaction(transaction, "ExecutionException", ex.Message);
        }
        finally
        {
            await _lockManager.ReleaseLockAsync(sim.DevicePortId);
            await _notifier.NotifyPortStatusChangedAsync(sim.DevicePort, cancellationToken);
        }
    }

    private static string EvaluatePlaceholders(string template, SimCard sim, Dictionary<string, string> parameters, decimal? amount)
    {
        var result = template;

        // Password / PIN replacement (matches legacy GoIP128Api: item.UssdDescription.Replace("Password", line.Password))
        if (!string.IsNullOrEmpty(sim.WalletPassword))
        {
            result = result.Replace("{Password}", sim.WalletPassword)
                           .Replace("Password", sim.WalletPassword);
        }

        // Amount replacement (matches legacy GoIP128Api: item.UssdDescription.Replace("Amount", ((int)ServiceAmount).ToString("0")))
        if (amount.HasValue)
        {
            var amountStr = amount.Value.ToString("0.##");
            result = result.Replace("{Amount}", amountStr)
                           .Replace("Amount", amountStr);
        }

        // Target Phone / MobileNumber replacement
        if (parameters.TryGetValue("phoneNumber", out var phone) ||
            parameters.TryGetValue("MobileNumber", out phone) ||
            parameters.TryGetValue("mobile", out phone))
        {
            result = result.Replace("{MobileNumber}", phone)
                           .Replace("MobileNumber", phone)
                           .Replace("{PhoneNumber}", phone);
        }
        else if (!string.IsNullOrEmpty(sim.PhoneNumber))
        {
            result = result.Replace("{PhoneNumber}", sim.PhoneNumber);
        }

        // Merchant Code replacement
        if (parameters.TryGetValue("merchantCode", out var merchant) ||
            parameters.TryGetValue("MerchantCode", out merchant))
        {
            result = result.Replace("{MerchantCode}", merchant)
                           .Replace("MerchantCode", merchant);
        }

        // Custom parameters passed in dictionary
        foreach (var kvp in parameters)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private Result<TransactionResultDto> FailTransaction(TenantTransaction tx, string code, string message)
    {
        tx.Status = TransactionStatus.Failed;
        tx.ErrorCode = code;
        tx.ErrorMessage = message;
        tx.CompletedAt = DateTime.UtcNow;
        _context.SaveChangesAsync();
        _notifier.NotifyTransactionUpdatedAsync(tx);
        return Result.Failure<TransactionResultDto>(code, message);
    }
}
