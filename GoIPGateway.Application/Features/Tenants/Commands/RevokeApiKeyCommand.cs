using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record RevokeApiKeyCommand(int ApiKeyId, int TenantId) : IRequest<Result<bool>>;

public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public RevokeApiKeyCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.RevokeApiKeyAsync(request.ApiKeyId, request.TenantId, cancellationToken);
    }
}
