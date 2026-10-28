using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using GoIPGateway.Domain.Enums;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Commands;

public record RegisterDeviceCommand(
    string DeviceName,
    DeviceType DeviceType,
    string IpAddress,
    int Port,
    string Username,
    string Password,
    string? Location = null,
    string? Notes = null
) : IRequest<Result<int>>;

public class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceName).NotEmpty().WithMessage("اسم الجهاز مطلوب").MaximumLength(200);
        RuleFor(x => x.IpAddress).NotEmpty().WithMessage("عنوان IP للجهاز مطلوب").MaximumLength(50);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).WithMessage("رقم المنفذ غير صالح");
        RuleFor(x => x.Username).NotEmpty().WithMessage("اسم المستخدم مطلوب");
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة");
    }
}

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result<int>>
{
    private readonly IDeviceCommandService _commandService;

    public RegisterDeviceCommandHandler(IDeviceCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<int>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.RegisterDeviceAsync(request, cancellationToken);
    }
}
