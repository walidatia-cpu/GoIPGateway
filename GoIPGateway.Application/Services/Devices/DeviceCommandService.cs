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
        var totalPorts = command.DeviceType == DeviceType.GoIP512 ? 512 : 128;
        int slotsPerBlock = command.DeviceType == DeviceType.GoIP512 ? 8 : 4;

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
            MerchantCode = command.MerchantCode?.Trim(),
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
            int block = ((i - 1) / slotsPerBlock) + 1;
            int slot = ((i - 1) % slotsPerBlock) + 1;
            char letter = (char)('A' + (slot - 1));
            ports.Add(new DevicePort
            {
                DeviceId = device.Id,
                PortNumber = i,
                PortLabel = $"{block}.{slot:D2} ({block}{letter})",
                Status = PortStatus.Empty,
                IsBusy = false,
                IsActive = (slot == 1), // Default: Group A (01) is active, other slots are standby
                CreatedDate = DateTime.UtcNow
            });
        }

        await _unitOfWork.DevicePorts.AddRangeAsync(ports, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(device.Id);
    }

    public async Task<Result<bool>> UpdateDeviceAsync(int id, string deviceName, string ipAddress, int port, string username, string password, string? location, string? notes, bool isActive, string? merchantCode = null, CancellationToken cancellationToken = default)
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
        device.MerchantCode = merchantCode?.Trim();
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

    public async Task<Result<bool>> RestoreDeviceAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices.GetByIdAsync(id, cancellationToken);
        if (device == null)
            return Result.Failure<bool>("Device.NotFound", "الجهاز غير موجود.");

        device.IsDelete = false;
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

    public async Task<Result<bool>> SwitchActiveGroupAsync(int deviceId, string group, CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices.GetByIdAsync(deviceId, cancellationToken);
        int slotsPerBlock = (device?.DeviceType == DeviceType.GoIP512) ? 8 : 4;

        int targetSlot = (group ?? "").Trim().ToUpperInvariant() switch
        {
            "A" or "01" or "1" => 1,
            "B" or "02" or "2" => 2,
            "C" or "03" or "3" => 3,
            "D" or "04" or "4" => 4,
            "E" or "05" or "5" => 5,
            "F" or "06" or "6" => 6,
            "G" or "07" or "7" => 7,
            "H" or "08" or "8" => 8,
            _ => 1
        };

        var ports = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _unitOfWork.DevicePorts.AsQueryable()
                .Where(p => p.DeviceId == deviceId),
            cancellationToken);

        if (ports.Count == 0)
            return Result.Failure<bool>("Device.NoPorts", "لا توجد منافذ مسجلة لهذا الجهاز.");

        foreach (var p in ports)
        {
            int block = ((p.PortNumber - 1) / slotsPerBlock) + 1;
            int slot = ((p.PortNumber - 1) % slotsPerBlock) + 1;
            char letter = (char)('A' + (slot - 1));

            // Standard GoIP notation: e.g. "1.01 (1A)"
            p.PortLabel = $"{block}.{slot:D2} ({block}{letter})";

            // Only 1 slot active per block!
            p.IsActive = (slot == targetSlot);
            p.Status = p.IsActive
                ? (p.SimCard != null ? PortStatus.Active : PortStatus.Empty)
                : PortStatus.Disabled;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }

    public async Task<Result<bool>> SwitchBlockSlotAsync(int deviceId, int portId, CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices.GetByIdAsync(deviceId, cancellationToken);
        int slotsPerBlock = (device?.DeviceType == DeviceType.GoIP512) ? 8 : 4;

        var targetPort = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            _unitOfWork.DevicePorts.AsQueryable()
                .Where(p => p.Id == portId && p.DeviceId == deviceId),
            cancellationToken);

        if (targetPort == null)
            return Result.Failure<bool>("Port.NotFound", "المنفذ غير موجود.");

        int targetBlock = ((targetPort.PortNumber - 1) / slotsPerBlock) + 1;

        var allPorts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _unitOfWork.DevicePorts.AsQueryable()
                .Where(p => p.DeviceId == deviceId),
            cancellationToken);

        var blockSiblings = allPorts.Where(p => ((p.PortNumber - 1) / slotsPerBlock) + 1 == targetBlock).ToList();

        foreach (var p in blockSiblings)
        {
            int block = ((p.PortNumber - 1) / slotsPerBlock) + 1;
            int slot = ((p.PortNumber - 1) % slotsPerBlock) + 1;
            char letter = (char)('A' + (slot - 1));

            p.PortLabel = $"{block}.{slot:D2} ({block}{letter})";
            p.IsActive = (p.Id == portId);
            p.Status = p.IsActive
                ? (p.SimCard != null ? PortStatus.Active : PortStatus.Empty)
                : PortStatus.Disabled;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
