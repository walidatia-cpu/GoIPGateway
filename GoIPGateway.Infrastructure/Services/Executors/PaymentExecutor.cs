using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Factories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.Services.Executors;

/// <summary>
/// مشغّل عمليات الدفع والتحويل النقدي والمحافظ الإلكترونية و QR Code
/// يدعم التحقق الاستباقي من الرصيد، حجز المنفذ، بناء أوامر الـ USSD المخصصة أو التلقائية، والخصم المالي الآمن
/// </summary>
public class PaymentExecutor : IServiceExecutor
{
    private readonly IAppDbContext _context;
    private readonly IGoipCommunicationClient _goipClient;
    private readonly IPortLockManager _lockManager;
    private readonly ISignalRNotifier _notifier;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ILogger<PaymentExecutor> _logger;

    public PaymentExecutor(
        IAppDbContext context,
        IGoipCommunicationClient goipClient,
        IPortLockManager lockManager,
        ISignalRNotifier notifier,
        IWebhookDispatcher webhookDispatcher,
        ILogger<PaymentExecutor> logger)
    {
        _context = context;
        _goipClient = goipClient;
        _lockManager = lockManager;
        _notifier = notifier;
        _webhookDispatcher = webhookDispatcher;
        _logger = logger;
    }

    public bool CanHandle(ServiceType serviceType) =>
        serviceType is ServiceType.QRPayment or ServiceType.Transfer or ServiceType.Settlement;

    public async Task<Result<TransactionResultDto>> ExecuteAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (!transaction.DevicePortId.HasValue || !transaction.SimCardId.HasValue)
        {
            return await FailTransactionAsync(transaction, "MissingPortOrSim", "لم يتم تحديد منفذ أو شريحة صالحة لتنفيذ عملية الدفع.", cancellationToken);
        }

        var sim = await _context.SimCards
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .FirstOrDefaultAsync(s => s.Id == transaction.SimCardId.Value, cancellationToken);

        if (sim == null || sim.DevicePort == null || sim.DevicePort.Device == null)
        {
            return await FailTransactionAsync(transaction, "DeviceUnavailable", "جهاز GoIP أو الشريحة غير متوفرين على الشبكة.", cancellationToken);
        }

        var service = await _context.ServiceDefinitions
            .Include(s => s.UssdSteps.OrderBy(st => st.StepOrder))
            .FirstOrDefaultAsync(s => s.Id == transaction.ServiceDefinitionId, cancellationToken);

        if (service == null)
        {
            return await FailTransactionAsync(transaction, "ServiceNotFound", "بيانات الخدمة غير موجودة.", cancellationToken);
        }

        // 1. Pre-validation of SIM Balance for Payments
        if (transaction.Amount.HasValue && transaction.Amount.Value > 0 && service.BalanceActionType == BalanceActionType.Decrement)
        {
            if (sim.CurrentBalance < transaction.Amount.Value)
            {
                var msg = $"رصيد المحفظة / الشريحة ({sim.CurrentBalance:N2} ج.م) غير كافٍ لتنفيذ عملية الدفع بمبلغ {transaction.Amount.Value:N2} ج.م.";
                return await FailTransactionAsync(transaction, "InsufficientBalance", msg, cancellationToken);
            }
        }

        // 2. Acquire Port Lock
        var locked = await _lockManager.TryAcquireLockAsync(sim.DevicePortId, TimeSpan.FromSeconds(25), cancellationToken);
        if (!locked)
        {
            return await FailTransactionAsync(transaction, "PortLocked", "منفذ الشريحة مشغول حالياً بعملية أخرى. يرجى إعادة المحاولة.", cancellationToken);
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

            // Scenario A: Custom steps defined in DB
            if (service.UssdSteps.Any(s => s.IsActive && !s.IsDelete))
            {
                var activeSteps = service.UssdSteps.Where(s => s.IsActive && !s.IsDelete).OrderBy(s => s.StepOrder).ToList();
                foreach (var step in activeSteps)
                {
                    stopwatch.Restart();
                    var command = EvaluatePlaceholders(step.UssdCommand, sim, parameters, transaction.Amount);

                    _logger.LogInformation("Payment Tx {TxRef} Step {Order}: Executing USSD: {Command}", transaction.TransactionRef, step.StepOrder, command);

                    var response = await _goipClient.SendUssdCommandAsync(
                        sim.DevicePort.Device,
                        sim.DevicePort.PortNumber,
                        command,
                        step.TimeoutSeconds > 0 ? step.TimeoutSeconds : 30,
                        cancellationToken);

                    stopwatch.Stop();
                    lastResponse = response;

                    var isStepSuccess = EvaluateStepSuccess(step, response, out var stepError, out var clientSafeMessage);
                    if (isStepSuccess && !string.IsNullOrWhiteSpace(clientSafeMessage))
                    {
                        finalSuccessClientMessage = clientSafeMessage;
                    }

                    var log = new TransactionLog
                    {
                        TransactionId = transaction.Id,
                        StepOrder = step.StepOrder,
                        StepName = step.StepTitle ?? $"خطوة دفع {step.StepOrder}",
                        UssdCommand = command,
                        UssdResponse = response,
                        IsSuccess = isStepSuccess,
                        ErrorMessage = stepError,
                        DurationMs = (int)stopwatch.ElapsedMilliseconds,
                        CreatedAt = DateTime.UtcNow
                    };

                    logs.Add(log);
                    _context.TransactionLogs.Add(log);
                    await _context.SaveChangesAsync(cancellationToken);

                    if (!isStepSuccess)
                    {
                        var finalErrorMessage = clientSafeMessage ?? stepError ?? "فشلت خطوة أثناء معالجة الدفع.";
                        return await MarkTransactionFailedAsync(transaction, response, "PaymentStepFailed", finalErrorMessage, cancellationToken);
                    }

                    if (step.DelayAfterMs > 0)
                    {
                        await Task.Delay(step.DelayAfterMs, cancellationToken);
                    }
                }
            }
            else
            {
                // Scenario B: Standard auto-constructed payment USSD
                var paymentCommand = BuildAutoPaymentUssdCommand(sim.Operator, sim, parameters, transaction.Amount);
                stopwatch.Restart();

                _logger.LogInformation("Payment Tx {TxRef}: Executing Auto USSD: {Command}", transaction.TransactionRef, paymentCommand);

                var response = await _goipClient.SendUssdCommandAsync(
                    sim.DevicePort.Device,
                    sim.DevicePort.PortNumber,
                    paymentCommand,
                    35,
                    cancellationToken);

                stopwatch.Stop();
                lastResponse = response;

                var isSuccess = DeterminePaymentSuccess(response, out var paymentError, out var successMsg);
                if (isSuccess)
                {
                    finalSuccessClientMessage = successMsg;
                }

                var log = new TransactionLog
                {
                    TransactionId = transaction.Id,
                    StepOrder = 1,
                    StepName = "أمر الدفع الإلكتروني المباشر",
                    UssdCommand = paymentCommand,
                    UssdResponse = response,
                    IsSuccess = isSuccess,
                    ErrorMessage = isSuccess ? null : paymentError,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    CreatedAt = DateTime.UtcNow
                };

                logs.Add(log);
                _context.TransactionLogs.Add(log);
                await _context.SaveChangesAsync(cancellationToken);

                if (!isSuccess)
                {
                    return await MarkTransactionFailedAsync(transaction, response, "PaymentFailed", paymentError ?? "فشلت عملية الدفع من الشبكة.", cancellationToken);
                }
            }

            // === PAYMENT COMPLETED SUCCESSFULLY ===
            transaction.Status = TransactionStatus.Completed;
            transaction.RawResponse = lastResponse;
            transaction.CompletedAt = DateTime.UtcNow;

            // Financial Balance Deduction
            if (service.BalanceActionType == BalanceActionType.Decrement && transaction.Amount.HasValue)
            {
                sim.CurrentBalance -= transaction.Amount.Value;
                sim.DailyConsumed += transaction.Amount.Value;
                sim.MonthlyConsumed += transaction.Amount.Value;
                sim.ModifiedDate = DateTime.UtcNow;
                transaction.SimBalanceAfter = sim.CurrentBalance;

                _context.SimBalanceHistories.Add(new SimBalanceHistory
                {
                    SimCardId = sim.Id,
                    BalanceBefore = transaction.SimBalanceBefore ?? 0,
                    BalanceAfter = sim.CurrentBalance,
                    ChangeAmount = -transaction.Amount.Value,
                    ChangeType = BalanceActionType.Decrement,
                    TransactionId = transaction.Id,
                    Notes = $"تنفيذ عملية دفع/تحويل رقم {transaction.TransactionRef} بمبلغ {transaction.Amount.Value:N2} ج.م",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            // SignalR Live Events & Webhook Dispatch
            await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);
            await _notifier.NotifySimBalanceChangedAsync(sim, cancellationToken);
            await _webhookDispatcher.DispatchTransactionWebhookAsync(transaction, cancellationToken);

            var resultDto = new TransactionResultDto
            {
                TransactionId = transaction.Id,
                TransactionRef = transaction.TransactionRef,
                ExternalRef = transaction.ExternalRef,
                Status = TransactionStatus.Completed,
                Message = finalSuccessClientMessage ?? "تمت عملية الدفع بنجاح.",
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
                    ErrorMessage = l.ErrorMessage,
                    DurationMs = l.DurationMs
                }).ToList()
            };

            return Result.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing Payment transaction {TxRef}", transaction.TransactionRef);
            return await MarkTransactionFailedAsync(transaction, ex.Message, "PaymentException", "حدث خطأ غير متوقع أثناء معالجة الدفع: " + ex.Message, cancellationToken);
        }
        finally
        {
            await _lockManager.ReleaseLockAsync(sim.DevicePortId);
        }
    }

    private string BuildAutoPaymentUssdCommand(OperatorType op, SimCard sim, Dictionary<string, string> parameters, decimal? amount)
    {
        var amt = amount?.ToString("0.##") ?? parameters.GetValueOrDefault("amount", "0");
        var pin = sim.WalletPassword ?? sim.UssdPassword ?? parameters.GetValueOrDefault("pin", parameters.GetValueOrDefault("walletPassword", "0000"));
        var target = parameters.GetValueOrDefault("merchantCode", parameters.GetValueOrDefault("phoneNumber", parameters.GetValueOrDefault("receiverPhone", "")));

        return op switch
        {
            OperatorType.Vodafone => $"*9*7*{target}*{amt}*{pin}#",
            OperatorType.Orange => $"#115*1*{target}*{amt}*{pin}#",
            OperatorType.Etisalat => $"*777*1*{target}*{amt}*{pin}#",
            OperatorType.WE => $"*322*1*{target}*{amt}*{pin}#",
            _ => $"*9*7*{target}*{amt}*{pin}#"
        };
    }

    private static bool DeterminePaymentSuccess(string response, out string? errorMessage, out string? successMessage)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            errorMessage = "لم يتم استلام أي رد من الشبكة.";
            successMessage = null;
            return false;
        }

        var normalized = response.Trim();

        // Failure indicators
        if (normalized.Contains("خطأ") || normalized.Contains("غير كاف") || normalized.Contains("غير صحيح") ||
            normalized.Contains("فشل") || normalized.Contains("رفض") || normalized.Contains("incorrect") ||
            normalized.Contains("failed") || normalized.Contains("insufficient") || normalized.Contains("invalid"))
        {
            errorMessage = normalized;
            successMessage = null;
            return false;
        }

        // Success indicators
        if (normalized.Contains("تمت") || normalized.Contains("بنجاح") || normalized.Contains("success") ||
            normalized.Contains("تم تحويل") || normalized.Contains("رقم العملية") || normalized.Contains("تم الدفع"))
        {
            errorMessage = null;
            successMessage = normalized;
            return true;
        }

        // Fallback: accept response if no failure markers
        errorMessage = null;
        successMessage = "تم تنفيذ عملية الدفع بنجاح.";
        return true;
    }

    private static bool EvaluateStepSuccess(ServiceUssdStep step, string response, out string? stepError, out string? clientSafeMessage)
    {
        var failureRules = step.ParsedFailureRules;
        var matchedFailure = failureRules.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Pattern) && response.Contains(f.Pattern, StringComparison.OrdinalIgnoreCase));

        if (matchedFailure != null)
        {
            stepError = $"فشلت الخطوة: الاستجابة تطابق نمط الفشل ({matchedFailure.Pattern})";
            clientSafeMessage = !string.IsNullOrWhiteSpace(matchedFailure.ClientMessage) ? matchedFailure.ClientMessage : matchedFailure.Pattern;
            return false;
        }

        var successRules = step.ParsedSuccessRules;
        var noWaitRule = successRules.FirstOrDefault(s => s.Pattern.Equals("NoNeedToWait", StringComparison.OrdinalIgnoreCase));
        if (noWaitRule != null)
        {
            stepError = null;
            clientSafeMessage = noWaitRule.ClientMessage;
            return true;
        }

        if (successRules.Any())
        {
            var matchedSuccess = successRules.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Pattern) && response.Contains(s.Pattern, StringComparison.OrdinalIgnoreCase));
            if (matchedSuccess != null)
            {
                stepError = null;
                clientSafeMessage = matchedSuccess.ClientMessage;
                return true;
            }

            stepError = "استجابة الـ USSD لا تطابق أياً من عبارات النجاح.";
            clientSafeMessage = "تعذر تأكيد استجابة الدفع من الشبكة.";
            return false;
        }

        stepError = null;
        clientSafeMessage = null;
        return true;
    }

    private static string EvaluatePlaceholders(string commandTemplate, SimCard sim, Dictionary<string, string> parameters, decimal? amount)
    {
        var result = commandTemplate;
        result = result.Replace("{PhoneNumber}", sim.PhoneNumber ?? "")
                       .Replace("{SimNumber}", sim.SimNumber ?? "")
                       .Replace("{WalletPassword}", sim.WalletPassword ?? "")
                       .Replace("{UssdPassword}", sim.UssdPassword ?? "")
                       .Replace("{Pin}", sim.WalletPassword ?? sim.UssdPassword ?? "");

        if (amount.HasValue)
        {
            result = result.Replace("{Amount}", amount.Value.ToString("0.##"));
        }

        foreach (var (key, value) in parameters)
        {
            result = result.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private async Task<Result<TransactionResultDto>> MarkTransactionFailedAsync(TenantTransaction transaction, string? rawResponse, string errorCode, string errorMessage, CancellationToken cancellationToken)
    {
        transaction.Status = TransactionStatus.Failed;
        transaction.RawResponse = rawResponse;
        transaction.ErrorCode = errorCode;
        transaction.ErrorMessage = errorMessage;
        transaction.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);
        await _webhookDispatcher.DispatchTransactionWebhookAsync(transaction, cancellationToken);

        return Result.Failure<TransactionResultDto>(errorCode, errorMessage);
    }

    private async Task<Result<TransactionResultDto>> FailTransactionAsync(TenantTransaction transaction, string errorCode, string errorMessage, CancellationToken cancellationToken)
    {
        transaction.Status = TransactionStatus.Failed;
        transaction.ErrorCode = errorCode;
        transaction.ErrorMessage = errorMessage;
        transaction.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyTransactionUpdatedAsync(transaction, cancellationToken);
        await _webhookDispatcher.DispatchTransactionWebhookAsync(transaction, cancellationToken);

        return Result.Failure<TransactionResultDto>(errorCode, errorMessage);
    }
}
