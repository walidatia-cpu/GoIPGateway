using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record GenerateApiKeyCommand(int TenantId, string KeyName, int RateLimit = 100) : IRequest<Result<ApiKeyDto>>;

public class GenerateApiKeyCommandValidator : AbstractValidator<GenerateApiKeyCommand>
{
    public GenerateApiKeyCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0).WithMessage("معرف الشركة مطلوب");
        RuleFor(x => x.KeyName).NotEmpty().WithMessage("اسم المفتاح مطلوب");
        RuleFor(x => x.RateLimit).GreaterThan(0).WithMessage("معدل الطلبات يجب أن يكون أكبر من 0");
    }
}

public class GenerateApiKeyCommandHandler : IRequestHandler<GenerateApiKeyCommand, Result<ApiKeyDto>>
{
    private readonly ITenantCommandService _commandService;

    public GenerateApiKeyCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<ApiKeyDto>> Handle(GenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.GenerateApiKeyAsync(request.TenantId, request.KeyName, request.RateLimit, cancellationToken);
    }
}
