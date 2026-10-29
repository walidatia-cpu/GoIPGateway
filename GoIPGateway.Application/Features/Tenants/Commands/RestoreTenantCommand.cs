using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record RestoreTenantCommand(int Id) : IRequest<Result<bool>>;

public class RestoreTenantCommandHandler : IRequestHandler<RestoreTenantCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public RestoreTenantCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(RestoreTenantCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.RestoreTenantAsync(request.Id, cancellationToken);
    }
}
