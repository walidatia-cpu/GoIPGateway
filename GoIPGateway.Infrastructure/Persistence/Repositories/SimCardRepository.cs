using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Persistence.Repositories;

public class SimCardRepository : GenericRepository<SimCard, int>, ISimCardRepository
{
    public SimCardRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SimCard?> GetWithPortAndDeviceAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete, cancellationToken);
    }

    public async Task<IReadOnlyList<SimCard>> GetAllWithPortAndDeviceAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .Where(s => !s.IsDelete)
            .OrderBy(s => s.DevicePort.PortNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SimCard>> GetByOperatorAsync(OperatorType op, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .Where(s => s.Operator == op && !s.IsDelete && s.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SimCard>> GetAssignedToTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenantDevices = await _context.TenantDevices
            .Where(td => td.TenantId == tenantId && td.IsActive)
            .ToListAsync(cancellationToken);

        var deviceIds = tenantDevices.Select(td => td.DeviceId).ToList();

        return await _dbSet
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .Where(s => !s.IsDelete && deviceIds.Contains(s.DevicePort.DeviceId))
            .OrderBy(s => s.DevicePort.PortNumber)
            .ToListAsync(cancellationToken);
    }
}
