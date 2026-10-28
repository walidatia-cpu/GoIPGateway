using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record CreateTenantCommand(
    string CompanyName,
    string? CompanyNameEn,
    string? ContactPerson,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    int MaxDevices = 1,
    int MaxDailyOperations = 1000,
    decimal CreditLimit = 0
) : IRequest<Result<int>>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("اسم الشركة مطلوب").MaximumLength(200);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.MaxDevices).GreaterThan(0).WithMessage("عدد الأجهزة يجب أن يكون أكبر من 0");
        RuleFor(x => x.MaxDailyOperations).GreaterThan(0).WithMessage("حد العمليات اليومي يجب أن يكون أكبر من 0");
    }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<int>>
{
    private readonly ITenantCommandService _commandService;

    public CreateTenantCommandHandler(ITenantCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<int>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.CreateTenantAsync(request, cancellationToken);
    }
}
