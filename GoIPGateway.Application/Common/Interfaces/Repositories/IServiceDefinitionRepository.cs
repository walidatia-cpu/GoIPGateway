using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Common.Interfaces.Repositories;

public interface IServiceDefinitionRepository : IRepository<ServiceDefinition, int>
{
    Task<ServiceDefinition?> GetWithStepsAndParametersAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDefinition>> GetAllWithStepsAndParametersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDefinition>> GetSubscribedByTenantAsync(int tenantId, CancellationToken cancellationToken = default);
}
