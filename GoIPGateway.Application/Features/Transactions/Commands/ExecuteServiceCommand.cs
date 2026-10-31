using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Services.Transactions;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Application.Common.Interfaces;
using MediatR;

namespace GoIPGateway.Application.Features.Transactions.Commands;

public record ExecuteServiceCommand(
    string ServiceCode,
    string? ExternalRef,
    int? SimCardId,
    OperatorType? Operator,
    Dictionary<string, string> Parameters,
    string? CallbackUrl = null,
    RequestSource Source = RequestSource.API
) : IRequest<Result<TransactionResultDto>>, ITenantScopedRequest;

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
    private readonly ITransactionCommandService _commandService;

    public ExecuteServiceCommandHandler(ITransactionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<TransactionResultDto>> Handle(ExecuteServiceCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.ExecuteServiceAsync(request, cancellationToken);
    }
}
