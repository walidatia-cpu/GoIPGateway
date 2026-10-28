using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.Devices.Commands;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Services.Devices;

public class DeviceCommandService : IDeviceCommandService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeviceCommandService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> RegisterDeviceAsync(RegisterDeviceCommand command, CancellationToken cancellationToken = default)
    {
        var totalPorts = command.DeviceType switch
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
            DeviceName = command.DeviceName,
            DeviceType = command.DeviceType,
            IpAddress = command.IpAddress,
            Port = command.Port,
            HttpPort = command.Port,
            Username = command.Username,
            Password = command.Password,
            TotalPorts = totalPorts,
            Location = command.Location,
            Notes = command.Notes,
            IsActive = true,
            IsOnline = false,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Devices.AddAsync(device, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        await _unitOfWork.DevicePorts.AddRangeAsync(ports, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(device.Id);
    }

    public async Task<Result<bool>> UpdateDeviceAsync(int id, string deviceName, string ipAddress, int port, string username, string password, string? location, string? notes, bool isActive, CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices.GetByIdAsync(id, cancellationToken);
        if (device == null)
            return Result.Failure<bool>("Device.NotFound", "الجهاز غير موجود.");

        device.DeviceName = deviceName;
        device.IpAddress = ipAddress;
        device.Port = port;
        device.HttpPort = port;
        device.Username = username;
        if (!string.IsNullOrWhiteSpace(password)) device.Password = password;
        device.Location = location;
        device.Notes = notes;
        device.IsActive = isActive;
        device.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.Devices.Update(device);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> DeleteDeviceAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices.GetByIdAsync(id, cancellationToken);
        if (device == null)
            return Result.Failure<bool>("Device.NotFound", "الجهاز غير موجود.");

        device.IsDelete = true;
        device.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.Devices.Update(device);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> AssignDeviceToTenantAsync(int deviceId, int tenantId, int? portStart, int? portEnd, CancellationToken cancellationToken = default)
    {
        var tenantDevicesRepo = _unitOfWork.Repository<TenantDevice, int>();
        var existing = await tenantDevicesRepo.FirstOrDefaultAsync(td => td.DeviceId == deviceId && td.TenantId == tenantId && td.IsActive, cancellationToken);
        if (existing != null)
        {
            existing.PortRangeStart = portStart;
            existing.PortRangeEnd = portEnd;
            tenantDevicesRepo.Update(existing);
        }
        else
        {
            var assignment = new TenantDevice
            {
                DeviceId = deviceId,
                TenantId = tenantId,
                PortRangeStart = portStart,
                PortRangeEnd = portEnd,
                IsActive = true,
                AssignedDate = DateTime.UtcNow
            };
            await tenantDevicesRepo.AddAsync(assignment, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }

    public async Task<Result<bool>> UnassignDeviceFromTenantAsync(int deviceId, int tenantId, CancellationToken cancellationToken = default)
    {
        var tenantDevicesRepo = _unitOfWork.Repository<TenantDevice, int>();
        var existing = await tenantDevicesRepo.FirstOrDefaultAsync(td => td.DeviceId == deviceId && td.TenantId == tenantId, cancellationToken);
        if (existing != null)
        {
            existing.IsActive = false;
            tenantDevicesRepo.Update(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(true);
    }
}
