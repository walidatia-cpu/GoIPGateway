using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record UpdateTenantCommand(
    int Id,
    string CompanyName,
    string? CompanyNameEn,
    string? ContactPerson,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string? TaxId,
    int MaxDevices,
    int MaxDailyOperations,
    int MaxMonthlyOps,
    decimal CreditLimit,
    bool IsActive,
    bool IsBlocked,
    string? BlockReason
) : IRequest<Result<bool>>;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("اسم الشركة مطلوب").MaximumLength(200);
        RuleFor(x => x.MaxDevices).GreaterThan(0);
        RuleFor(x => x.MaxDailyOperations).GreaterThan(0);
    }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, Result<bool>>
{
    private readonly ITenantCommandService _commandService;

    public UpdateTenantCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateTenantAsync(
            request.Id,
            request.CompanyName,
            request.CompanyNameEn,
            request.ContactPerson,
            request.ContactEmail,
            request.ContactPhone,
            request.Address,
            request.TaxId,
            request.MaxDevices,
            request.MaxDailyOperations,
            request.MaxMonthlyOps,
            request.CreditLimit,
            request.IsActive,
            request.IsBlocked,
            request.BlockReason,
            cancellationToken);
    }
}
