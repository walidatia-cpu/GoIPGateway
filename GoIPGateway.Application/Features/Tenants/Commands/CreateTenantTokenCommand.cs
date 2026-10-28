using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record CreateTenantTokenCommand(
    int TenantId,
    string KeyName,
    int[]? AllowedServiceIds,
    int RateLimit = 100,
    int ExpiryDays = 365
) : IRequest<Result<bool>>;

public class CreateTenantTokenCommandValidator : AbstractValidator<CreateTenantTokenCommand>
{
    public CreateTenantTokenCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.RateLimit).GreaterThan(0);
        RuleFor(x => x.ExpiryDays).GreaterThan(0);
    }
}

public class CreateTenantTokenCommandHandler : IRequestHandler<CreateTenantTokenCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public CreateTenantTokenCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(CreateTenantTokenCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.CreateTenantTokenAsync(
            request.TenantId,
            request.KeyName,
            request.AllowedServiceIds,
            request.RateLimit,
            request.ExpiryDays,
            cancellationToken);
    }
}
