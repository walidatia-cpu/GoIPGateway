using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Common.Interfaces.Repositories;

public interface ISimCardRepository : IRepository<SimCard, int>
{
    Task<SimCard?> GetWithPortAndDeviceAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimCard>> GetAllWithPortAndDeviceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimCard>> GetByOperatorAsync(OperatorType op, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimCard>> GetAssignedToTenantAsync(int tenantId, CancellationToken cancellationToken = default);
}
