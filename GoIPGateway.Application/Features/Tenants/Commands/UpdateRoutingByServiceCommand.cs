using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record UpdateRoutingByServiceCommand(
    int TenantId,
    int ServiceDefinitionId,
    int[]? SelectedSimIds
) : IRequest<Result<bool>>;

public class UpdateRoutingByServiceCommandValidator : AbstractValidator<UpdateRoutingByServiceCommand>
{
    public UpdateRoutingByServiceCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.ServiceDefinitionId).GreaterThan(0);
    }
}

public class UpdateRoutingByServiceCommandHandler : IRequestHandler<UpdateRoutingByServiceCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public UpdateRoutingByServiceCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateRoutingByServiceCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateRoutingByServiceAsync(
            request.TenantId,
            request.ServiceDefinitionId,
            request.SelectedSimIds,
            cancellationToken);
    }
}
