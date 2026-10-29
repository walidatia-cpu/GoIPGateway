using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.ServiceDefinitions;

public class ServiceDefinitionCommandService : IServiceDefinitionCommandService
{
    private readonly IUnitOfWork _unitOfWork;

    public ServiceDefinitionCommandService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> CreateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Services.AsQueryable().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.ServiceCode.ToLower() == service.ServiceCode.ToLower(), cancellationToken);

        if (existing != null)
        {
            if (existing.IsDelete)
            {
                existing.IsDelete = false;
                existing.NameAr = service.NameAr;
                existing.NameEn = service.NameEn;
                existing.Description = service.Description;
                existing.Category = service.Category;
                existing.BalanceActionType = service.BalanceActionType;
                existing.ServiceType = ServiceType.USSD;
                existing.BaseCost = service.BaseCost;
                existing.Operator = service.Operator;
                existing.IconClass = service.IconClass;
                existing.SortOrder = service.SortOrder;
                existing.IsActive = service.IsActive;
                existing.ModifiedDate = DateTime.UtcNow;

                _unitOfWork.Services.Update(existing);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success(existing.Id);
            }

            return Result.Failure<int>("Service.DuplicateCode", $"كود الخدمة '{service.ServiceCode}' مسجل بالفعل.");
        }

        service.CreatedDate = DateTime.UtcNow;
        service.IsActive = true;

        await _unitOfWork.Services.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(service.Id);
    }

    public async Task<Result<bool>> UpdateServiceAsync(int id, ServiceDefinition updated, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (existing == null || existing.IsDelete)
            return Result.Failure<bool>("Service.NotFound", "الخدمة غير موجودة.");

        existing.NameAr = updated.NameAr;
        existing.NameEn = updated.NameEn;
        existing.ServiceCode = updated.ServiceCode;
        existing.Description = updated.Description;
        existing.ServiceType = updated.ServiceType;
        existing.Category = updated.Category;
        existing.Operator = updated.Operator;
        existing.BalanceActionType = updated.BalanceActionType;
        existing.BaseCost = updated.BaseCost;
        existing.IsActive = updated.IsActive;
        existing.RequiresApproval = updated.RequiresApproval;
        existing.IsSystemService = updated.IsSystemService;
        existing.SortOrder = updated.SortOrder;
        existing.IconClass = updated.IconClass;
        existing.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.Services.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> DeleteServiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null || service.IsDelete)
            return Result.Failure<bool>("Service.NotFound", "الخدمة غير موجودة.");

        service.IsDelete = true;
        service.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> ToggleServiceStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null || service.IsDelete)
            return Result.Failure<bool>("Service.NotFound", "الخدمة غير موجودة.");

        service.IsActive = !service.IsActive;
        service.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(service.IsActive);
    }

    public async Task<Result<int>> AddStepAsync(int serviceId, ServiceUssdStep step, CancellationToken cancellationToken = default)
    {
        step.ServiceDefinitionId = serviceId;
        step.CreatedDate = DateTime.UtcNow;
        step.IsActive = true;

        await _unitOfWork.ServiceSteps.AddAsync(step, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(step.Id);
    }

    public async Task<Result<bool>> UpdateStepAsync(int stepId, ServiceUssdStep step, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.ServiceSteps.GetByIdAsync(stepId, cancellationToken);
        if (existing == null || existing.IsDelete)
            return Result.Failure<bool>("Step.NotFound", "الخطوة غير موجودة.");

        existing.StepTitle = step.StepTitle;
        existing.UssdCommand = step.UssdCommand;
        existing.StepOrder = step.StepOrder;
        existing.SuccessPattern = step.SuccessPattern;
        existing.FailurePattern = step.FailurePattern;
        existing.TimeoutSeconds = step.TimeoutSeconds;
        existing.RetryCount = step.RetryCount;
        existing.DelayAfterMs = step.DelayAfterMs;
        existing.IsActive = step.IsActive;

        _unitOfWork.ServiceSteps.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> DeleteStepAsync(int stepId, int serviceId, CancellationToken cancellationToken = default)
    {
        var step = await _unitOfWork.ServiceSteps.FirstOrDefaultAsync(s => s.Id == stepId && s.ServiceDefinitionId == serviceId, cancellationToken);
        if (step == null || step.IsDelete)
            return Result.Failure<bool>("Step.NotFound", "الخطوة غير موجودة.");

        step.IsDelete = true;
        step.ModifiedDate = DateTime.UtcNow;

        _unitOfWork.ServiceSteps.Update(step);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<int>> AddParameterAsync(ServiceParameter param, CancellationToken cancellationToken = default)
    {
        param.ParameterKey = (param.ParameterKey ?? string.Empty).Trim().Trim('{', '}').Trim();
        if (string.IsNullOrWhiteSpace(param.ParameterKey))
            return Result.Failure<int>("Parameter.InvalidKey", "مفتاح البارامتر مطلوب.");

        if (string.IsNullOrWhiteSpace(param.ParameterName))
            param.ParameterName = char.ToUpper(param.ParameterKey[0]) + (param.ParameterKey.Length > 1 ? param.ParameterKey[1..] : "");

        if (string.IsNullOrWhiteSpace(param.DisplayNameAr))
            param.DisplayNameAr = param.ParameterName;

        var existing = await _unitOfWork.ServiceParameters.FirstOrDefaultAsync(
            p => p.ServiceDefinitionId == param.ServiceDefinitionId && 
                 p.ParameterKey.ToLower() == param.ParameterKey.ToLower() && 
                 !p.IsDelete, cancellationToken);

        if (existing != null)
        {
            return Result.Failure<int>("Parameter.Exists", $"مفتاح البارامتر '{param.ParameterKey}' مضاف مسبقاً لهذه الخدمة.");
        }

        param.IsActive = true;
        await _unitOfWork.ServiceParameters.AddAsync(param, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(param.Id);
    }

    public async Task<Result<bool>> DeleteParameterAsync(int paramId, int serviceId, CancellationToken cancellationToken = default)
    {
        var param = await _unitOfWork.ServiceParameters.FirstOrDefaultAsync(p => p.Id == paramId && p.ServiceDefinitionId == serviceId, cancellationToken);
        if (param == null)
            return Result.Failure<bool>("Parameter.NotFound", "البارامتر غير موجود.");

        _unitOfWork.ServiceParameters.Remove(param);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
