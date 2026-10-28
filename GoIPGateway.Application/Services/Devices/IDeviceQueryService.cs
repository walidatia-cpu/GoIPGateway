using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Services.Devices;

public interface IDeviceQueryService
{
    Task<Result<Device>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Device>> GetWithPortsAndSimsAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Device>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Device>>> GetAllWithPortsAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Device>>> GetTenantDevicesAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TenantDevice>>> GetTenantDeviceAssignmentsAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DevicePort>>> GetAvailablePortsAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DevicePort>>> GetCompanyEmptyPortsAsync(int tenantId, CancellationToken cancellationToken = default);
}


