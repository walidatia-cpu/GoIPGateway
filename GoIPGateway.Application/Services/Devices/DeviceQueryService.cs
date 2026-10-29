using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.Devices;

public class DeviceQueryService : IDeviceQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeviceQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Device>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices.GetByIdAsync(id, cancellationToken);
        if (device == null)
            return Result.Failure<Device>("Device.NotFound", "الجهاز غير موجود.");

        return Result.Success(device);
    }

    public async Task<Result<Device>> GetWithPortsAndSimsAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices.GetWithPortsAndSimsAsync(id, cancellationToken);
        if (device == null)
            return Result.Failure<Device>("Device.NotFound", "الجهاز غير موجود.");

        return Result.Success(device);
    }

    public async Task<Result<IReadOnlyList<Device>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _unitOfWork.Devices.GetAllAsync(cancellationToken);
        return Result.Success(devices);
    }

    public async Task<Result<IReadOnlyList<Device>>> GetAllWithPortsAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _unitOfWork.Devices.GetAllWithPortsAsync(cancellationToken);
        return Result.Success(devices);
    }

    public async Task<Result<IReadOnlyList<Device>>> GetTenantDevicesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var devices = await _unitOfWork.Devices.GetTenantDevicesAsync(tenantId, cancellationToken);
        return Result.Success(devices);
    }

    public async Task<Result<IReadOnlyList<TenantDevice>>> GetTenantDeviceAssignmentsAsync(int tenantId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<TenantDevice, int>().AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        query = query
            .Include(td => td.Device)
                .ThenInclude(d => d.Ports)
                    .ThenInclude(p => p.SimCard)
            .Where(td => td.TenantId == tenantId && td.IsActive);

        if (!includeDeleted)
        {
            query = query.Where(td => !td.Device.IsDelete);
        }

        var tenantDevices = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query, cancellationToken);
        return Result.Success<IReadOnlyList<TenantDevice>>(tenantDevices);
    }


    public async Task<Result<IReadOnlyList<DevicePort>>> GetAvailablePortsAsync(CancellationToken cancellationToken = default)
    {
        var assignedPortIds = (await _unitOfWork.SimCards.FindAsync(s => !s.IsDelete, cancellationToken))
            .Select(s => s.DevicePortId)
            .ToHashSet();

        var availablePorts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _unitOfWork.DevicePorts.AsQueryable()
                .Include(p => p.Device)
                .Where(p => !assignedPortIds.Contains(p.Id) && p.IsActive)
                .OrderBy(p => p.DeviceId).ThenBy(p => p.PortNumber),
            cancellationToken);

        return Result.Success<IReadOnlyList<DevicePort>>(availablePorts);
    }

    public async Task<Result<IReadOnlyList<DevicePort>>> GetCompanyEmptyPortsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var deviceIds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _unitOfWork.Repository<TenantDevice, int>().AsQueryable()
                .Where(td => td.TenantId == tenantId && td.IsActive && !td.Device.IsDelete)
                .Select(td => td.DeviceId),
            cancellationToken);

        var emptyPorts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _unitOfWork.DevicePorts.AsQueryable()
                .Include(p => p.Device)
                .Where(p => deviceIds.Contains(p.DeviceId) && p.SimCard == null && p.IsActive)
                .OrderBy(p => p.Device.DeviceName)
                .ThenBy(p => p.PortNumber),
            cancellationToken);

        return Result.Success<IReadOnlyList<DevicePort>>(emptyPorts);
    }
}


