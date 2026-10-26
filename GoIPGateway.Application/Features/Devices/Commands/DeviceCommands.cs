using FluentValidation;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        RuleFor(x => x.DeviceName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IpAddress).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result<int>>
{
    private readonly IAppDbContext _context;

    public RegisterDeviceCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var totalPorts = request.DeviceType switch
        {
            DeviceType.GoIP1 => 1,
            DeviceType.GoIP4 => 4,
            DeviceType.GoIP8 => 8,
            DeviceType.GoIP16 => 16,
            DeviceType.GoIP32 => 32,
            DeviceType.GoIP512 => 512,
            _ => 128
        };

        var device = new Device
        {
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            IpAddress = request.IpAddress,
            Port = request.Port,
            HttpPort = request.Port,
            Username = request.Username,
            Password = request.Password,
            TotalPorts = totalPorts,
            Location = request.Location,
            Notes = request.Notes,
            IsActive = true,
            IsOnline = false,
            CreatedDate = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync(cancellationToken);

        // Pre-create DevicePorts
        var ports = new List<DevicePort>();
        for (int i = 1; i <= totalPorts; i++)
        {
            ports.Add(new DevicePort
            {
                DeviceId = device.Id,
                PortNumber = i,
                PortLabel = $"L{i:D3}",
                Status = PortStatus.Empty,
                IsBusy = false,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });
        }

        _context.DevicePorts.AddRange(ports);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(device.Id);
    }
}
