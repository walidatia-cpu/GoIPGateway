using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record ToggleTenantBlockCommand(int TenantId, string? Reason = null) : IRequest<Result<bool>>;

public class ToggleTenantBlockCommandHandler : IRequestHandler<ToggleTenantBlockCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public ToggleTenantBlockCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(ToggleTenantBlockCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.ToggleBlockTenantAsync(request.TenantId, request.Reason, cancellationToken);
    }
}
