using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Services.SimCards;

public interface ISimCardQueryService
{
    Task<Result<SimCard>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<SimCard>> GetWithPortAndDeviceAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SimCard>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SimCard>>> GetAllWithPortAndDeviceAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SimCard>>> GetByOperatorAsync(OperatorType op, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SimCard>>> GetAssignedToTenantAsync(int tenantId, CancellationToken cancellationToken = default);
}
