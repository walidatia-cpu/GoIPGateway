using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Common.Interfaces.Repositories;

public interface IDeviceRepository : IRepository<Device, int>
{
    Task<Device?> GetWithPortsAndSimsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Device>> GetAllWithPortsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Device>> GetTenantDevicesAsync(int tenantId, CancellationToken cancellationToken = default);
}
