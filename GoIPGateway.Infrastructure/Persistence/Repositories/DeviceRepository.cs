using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Persistence.Repositories;

public class DeviceRepository : GenericRepository<Device, int>, IDeviceRepository
{
    public DeviceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Device?> GetWithPortsAndSimsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDelete, cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> GetAllWithPortsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .Where(d => !d.IsDelete)
            .OrderBy(d => d.DeviceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> GetTenantDevicesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .Include(d => d.AssignedTenants)
            .Where(d => !d.IsDelete && d.AssignedTenants.Any(at => at.TenantId == tenantId && at.IsActive))
            .ToListAsync(cancellationToken);
    }
}
