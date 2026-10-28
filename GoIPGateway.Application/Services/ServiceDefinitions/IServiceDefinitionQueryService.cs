using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Services.ServiceDefinitions;

public interface IServiceDefinitionQueryService
{
    Task<Result<ServiceDefinition>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ServiceDefinition>> GetWithStepsAndParametersAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ServiceDefinition>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ServiceDefinition>>> GetAllWithStepsAndParametersAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ServiceDefinition>>> GetSubscribedByTenantAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Result<ServiceUssdStep>> GetStepByIdAsync(int stepId, CancellationToken cancellationToken = default);
    Task<Result<ServiceParameter>> GetParameterByIdAsync(int paramId, CancellationToken cancellationToken = default);
}
