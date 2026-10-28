using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record DeleteApiKeyCommand(int ApiKeyId, int TenantId) : IRequest<Result<bool>>;

public class DeleteApiKeyCommandValidator : AbstractValidator<DeleteApiKeyCommand>
{
    public DeleteApiKeyCommandValidator()
    {
        RuleFor(x => x.ApiKeyId).GreaterThan(0);
        RuleFor(x => x.TenantId).GreaterThan(0);
    }
}

public class DeleteApiKeyCommandHandler : IRequestHandler<DeleteApiKeyCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public DeleteApiKeyCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(DeleteApiKeyCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.DeleteApiKeyAsync(request.ApiKeyId, request.TenantId, cancellationToken);
    }
}
