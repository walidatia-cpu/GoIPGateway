using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Services.Transactions;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Application.Common.Interfaces;
using MediatR;

namespace GoIPGateway.Application.Features.Transactions.Commands;

/// <summary>
/// أمر الدفع والتحويل الموحد عبر قناة api/v1/payments
/// لا يطلب باسورد الشريحة أو كود التاجر من العميل؛ كلاهما حقول داخلية يجلبها النظام تلقائياً
/// </summary>
public record ProcessPaymentCommand(
    string ServiceCode,
    decimal Amount,
    string? PhoneNumber = null,
    OperatorType? Operator = null,
    int? SimCardId = null,
    string? ExternalRef = null,
    string? CallbackUrl = null,
    Dictionary<string, string>? Parameters = null
) : IRequest<Result<TransactionResultDto>>, ITenantScopedRequest;

/// <summary>
/// التحقق الاستباقي للطلب قبل دخوله إلى السيرفس عبر FluentValidation داخل MediatR Pipeline
/// </summary>
public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithMessage("كود الخدمة مطلوب (ServiceCode is required).")
            .MaximumLength(50).WithMessage("كود الخدمة لا يجب أن يتجاوز 50 حرفاً.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ الدفع يجب أن يكون أكبر من الصفر.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(\+?20|0)?1[0125]\d{8}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("رقم الهاتف غير صالح، يجب أن يكون رقم محمول مصري صحيح.");

        RuleFor(x => x.CallbackUrl)
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("رابط الإشعار (Callback URL) غير صالح.");
    }
}

/// <summary>
/// معالج أمر الدفع الموحد: ينسق البارامترات ويمرر الأمر للمنسق المركزي
/// </summary>
public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, Result<TransactionResultDto>>
{
    private readonly ITransactionCommandService _transactionService;

    public ProcessPaymentCommandHandler(ITransactionCommandService transactionService)
    {
        _transactionService = transactionService;
    }

    public async Task<Result<TransactionResultDto>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>(request.Parameters ?? new(), StringComparer.OrdinalIgnoreCase);
        parameters["amount"] = request.Amount.ToString("0.##");

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !parameters.ContainsKey("phoneNumber"))
        {
            parameters["phoneNumber"] = request.PhoneNumber.Trim();
            parameters["recipient"] = request.PhoneNumber.Trim();
        }

        var command = new ExecuteServiceCommand(
            ServiceCode: request.ServiceCode.Trim(),
            ExternalRef: request.ExternalRef,
            SimCardId: request.SimCardId,
            Operator: request.Operator,
            Parameters: parameters,
            CallbackUrl: request.CallbackUrl,
            Source: RequestSource.API
        );

        return await _transactionService.ExecuteServiceAsync(command, cancellationToken);
    }
}

