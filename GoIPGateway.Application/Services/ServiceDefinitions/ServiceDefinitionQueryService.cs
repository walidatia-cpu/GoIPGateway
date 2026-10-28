using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.ServiceDefinitions;

public class ServiceDefinitionQueryService : IServiceDefinitionQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public ServiceDefinitionQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ServiceDefinition>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null || service.IsDelete)
            return Result.Failure<ServiceDefinition>("Service.NotFound", "الخدمة غير موجودة.");

        return Result.Success(service);
    }

    public async Task<Result<ServiceDefinition>> GetWithStepsAndParametersAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetWithStepsAndParametersAsync(id, cancellationToken);
        if (service == null)
            return Result.Failure<ServiceDefinition>("Service.NotFound", "الخدمة غير موجودة.");

        return Result.Success(service);
    }

    public async Task<Result<IReadOnlyList<ServiceDefinition>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetAllAsync(cancellationToken);
        return Result.Success(services);
    }

    public async Task<Result<IReadOnlyList<ServiceDefinition>>> GetAllWithStepsAndParametersAsync(CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetAllWithStepsAndParametersAsync(cancellationToken);
        return Result.Success(services);
    }

    public async Task<Result<IReadOnlyList<ServiceDefinition>>> GetSubscribedByTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetSubscribedByTenantAsync(tenantId, cancellationToken);
        return Result.Success(services);
    }

    public async Task<Result<ServiceUssdStep>> GetStepByIdAsync(int stepId, CancellationToken cancellationToken = default)
    {
        var step = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            _unitOfWork.ServiceSteps.AsQueryable()
                .Include(s => s.ServiceDefinition)
                    .ThenInclude(sd => sd.Parameters.Where(p => p.IsActive)),
            s => s.Id == stepId && !s.IsDelete,
            cancellationToken);

        if (step == null)
            return Result.Failure<ServiceUssdStep>("Step.NotFound", "خطوة الـ USSD غير موجودة.");

        return Result.Success(step);
    }

    public async Task<Result<ServiceParameter>> GetParameterByIdAsync(int paramId, CancellationToken cancellationToken = default)
    {
        var param = await _unitOfWork.ServiceParameters.GetByIdAsync(paramId, cancellationToken);
        if (param == null || !param.IsActive)
            return Result.Failure<ServiceParameter>("Parameter.NotFound", "البارامتر غير موجود.");

        return Result.Success(param);
    }
}
