using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Services.ServiceDefinitions;

public interface IServiceDefinitionCommandService
{
    Task<Result<int>> CreateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateServiceAsync(int id, ServiceDefinition updated, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteServiceAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> ToggleServiceStatusAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Result<int>> AddStepAsync(int serviceId, ServiceUssdStep step, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateStepAsync(int stepId, ServiceUssdStep step, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteStepAsync(int stepId, int serviceId, CancellationToken cancellationToken = default);

    Task<Result<int>> AddParameterAsync(ServiceParameter param, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteParameterAsync(int paramId, int serviceId, CancellationToken cancellationToken = default);
}
