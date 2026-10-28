using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record SaveTenantServicesCommand(
    int TenantId,
    int[]? SelectedServiceIds
) : IRequest<Result<bool>>;

public class SaveTenantServicesCommandValidator : AbstractValidator<SaveTenantServicesCommand>
{
    public SaveTenantServicesCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
    }
}

public class SaveTenantServicesCommandHandler : IRequestHandler<SaveTenantServicesCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public SaveTenantServicesCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(SaveTenantServicesCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.SaveTenantServicesAsync(request.TenantId, request.SelectedServiceIds, cancellationToken);
    }
}
