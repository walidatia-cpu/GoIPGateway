using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record UpdateRoutingBySimCommand(
    int TenantId,
    int SimId,
    int[]? SelectedServiceIds
) : IRequest<Result<bool>>;

public class UpdateRoutingBySimCommandValidator : AbstractValidator<UpdateRoutingBySimCommand>
{
    public UpdateRoutingBySimCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.SimId).GreaterThan(0);
    }
}

public class UpdateRoutingBySimCommandHandler : IRequestHandler<UpdateRoutingBySimCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public UpdateRoutingBySimCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateRoutingBySimCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateRoutingBySimAsync(
            request.TenantId,
            request.SimId,
            request.SelectedServiceIds,
            cancellationToken);
    }
}
