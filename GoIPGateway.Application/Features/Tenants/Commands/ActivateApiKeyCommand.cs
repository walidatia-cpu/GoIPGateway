using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record ActivateApiKeyCommand(int ApiKeyId, int TenantId) : IRequest<Result<bool>>;

public class ActivateApiKeyCommandValidator : AbstractValidator<ActivateApiKeyCommand>
{
    public ActivateApiKeyCommandValidator()
    {
        RuleFor(x => x.ApiKeyId).GreaterThan(0);
        RuleFor(x => x.TenantId).GreaterThan(0);
    }
}

public class ActivateApiKeyCommandHandler : IRequestHandler<ActivateApiKeyCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public ActivateApiKeyCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(ActivateApiKeyCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.ActivateApiKeyAsync(request.ApiKeyId, request.TenantId, cancellationToken);
    }
}
