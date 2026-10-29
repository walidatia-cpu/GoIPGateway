using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Devices;
using MediatR;

namespace GoIPGateway.Application.Features.Devices.Commands;

public record UpdateDeviceCommand(
    int Id,
    string DeviceName,
    string IpAddress,
    int Port,
    string Username,
    string Password,
    string? Location,
    string? Notes,
    bool IsActive,
    string? MerchantCode = null
) : IRequest<Result<bool>>;

public class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DeviceName).NotEmpty().WithMessage("اسم الجهاز مطلوب").MaximumLength(200);
        RuleFor(x => x.IpAddress).NotEmpty().WithMessage("عنوان IP للجهاز مطلوب").MaximumLength(50);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
    }
}

public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, Result<bool>>
{
    private readonly IDeviceCommandService _commandService;

    public UpdateDeviceCommandHandler(IDeviceCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateDeviceAsync(
            request.Id,
            request.DeviceName,
            request.IpAddress,
            request.Port,
            request.Username,
            request.Password,
            request.Location,
            request.Notes,
            request.IsActive,
            request.MerchantCode,
            cancellationToken);
    }
}
