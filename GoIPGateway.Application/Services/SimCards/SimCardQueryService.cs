using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Services.SimCards;

public class SimCardQueryService : ISimCardQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public SimCardQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SimCard>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sim = await _unitOfWork.SimCards.GetByIdAsync(id, cancellationToken);
        if (sim == null)
            return Result.Failure<SimCard>("Sim.NotFound", "الشريحة غير موجودة.");

        return Result.Success(sim);
    }

    public async Task<Result<SimCard>> GetWithPortAndDeviceAsync(int id, CancellationToken cancellationToken = default)
    {
        var sim = await _unitOfWork.SimCards.GetWithPortAndDeviceAsync(id, cancellationToken);
        if (sim == null)
            return Result.Failure<SimCard>("Sim.NotFound", "الشريحة غير موجودة.");

        return Result.Success(sim);
    }

    public async Task<Result<IReadOnlyList<SimCard>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sims = await _unitOfWork.SimCards.GetAllAsync(cancellationToken);
        return Result.Success(sims);
    }

    public async Task<Result<IReadOnlyList<SimCard>>> GetAllWithPortAndDeviceAsync(CancellationToken cancellationToken = default)
    {
        var sims = await _unitOfWork.SimCards.GetAllWithPortAndDeviceAsync(cancellationToken);
        return Result.Success(sims);
    }

    public async Task<Result<IReadOnlyList<SimCard>>> GetByOperatorAsync(OperatorType op, CancellationToken cancellationToken = default)
    {
        var sims = await _unitOfWork.SimCards.GetByOperatorAsync(op, cancellationToken);
        return Result.Success(sims);
    }

    public async Task<Result<IReadOnlyList<SimCard>>> GetAssignedToTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var sims = await _unitOfWork.SimCards.GetAssignedToTenantAsync(tenantId, cancellationToken);
        return Result.Success(sims);
    }
}
