using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Dashboard;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.Dashboard;

public class DashboardQueryService : IDashboardQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminDashboardDto>> GetAdminDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var tenantsList = await _unitOfWork.Tenants.AsQueryable()
            .Include(t => t.ApiKeys)
            .Include(t => t.Transactions.Where(tx => tx.RequestedAt >= today))
            .Where(t => !t.IsDelete)
            .ToListAsync(cancellationToken);

        var totalTenants = tenantsList.Count;
        var activeTenants = tenantsList.Count(t => t.IsActive && !t.IsBlocked);

        var devices = await _unitOfWork.Devices.AsQueryable()
            .Include(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .Where(d => !d.IsDelete)
            .ToListAsync(cancellationToken);

        var totalDevices = devices.Count;
        var onlineDevices = devices.Count(d => d.IsOnline);

        var allPorts = devices.SelectMany(d => d.Ports).ToList();
        var totalPorts = allPorts.Count;
        var busyPorts = allPorts.Count(p => p.IsBusy);

        var sims = allPorts.Where(p => p.SimCard != null && !p.SimCard.IsDelete).Select(p => p.SimCard!).ToList();
        var totalSims = sims.Count;
        var activeSims = sims.Count(s => s.IsActive && !s.IsBlocked);
        var totalSimBalance = sims.Sum(s => s.CurrentBalance);

        var todayTxs = await _unitOfWork.Transactions.AsQueryable()
            .Where(t => t.RequestedAt >= today)
            .ToListAsync(cancellationToken);

        var todayCount = todayTxs.Count;
        var todaySuccess = todayTxs.Count(t => t.Status == TransactionStatus.Completed);
        var todayFailed = todayTxs.Count(t => t.Status == TransactionStatus.Failed);

        var platformRevenue = todayTxs.Where(t => t.Status == TransactionStatus.Completed).Sum(t => t.TenantCharge);
        var platformVolume = todayTxs.Where(t => t.Status == TransactionStatus.Completed).Sum(t => t.Amount ?? 0);

        var recentTxs = await _unitOfWork.Transactions.AsQueryable()
            .Include(t => t.Tenant)
            .Include(t => t.ServiceDefinition)
            .Include(t => t.SimCard)
            .OrderByDescending(t => t.RequestedAt)
            .Take(15)
            .ToListAsync(cancellationToken);

        var topTenants = tenantsList.Select(t => new TenantUsageStatDto
        {
            TenantId = t.Id,
            CompanyName = t.CompanyName,
            IsActive = t.IsActive,
            ActiveApiKeysCount = t.ApiKeys.Count(k => k.IsActive),
            TodayTransactionsCount = t.Transactions.Count,
            SuccessfulTransactionsCount = t.Transactions.Count(tx => tx.Status == TransactionStatus.Completed),
            FailedTransactionsCount = t.Transactions.Count(tx => tx.Status == TransactionStatus.Failed),
            TotalVolume = t.Transactions.Where(tx => tx.Status == TransactionStatus.Completed).Sum(tx => tx.Amount ?? 0)
        })
        .OrderByDescending(t => t.TodayTransactionsCount)
        .Take(5)
        .ToList();

        var carrierStats = new List<CarrierStatDto>
        {
            new() { Operator = OperatorType.Vodafone, NameAr = "فودافون", SimCount = sims.Count(s => s.Operator == OperatorType.Vodafone), TodayOperations = todayTxs.Count(t => t.SimCard?.Operator == OperatorType.Vodafone) },
            new() { Operator = OperatorType.Orange, NameAr = "أورانج", SimCount = sims.Count(s => s.Operator == OperatorType.Orange), TodayOperations = todayTxs.Count(t => t.SimCard?.Operator == OperatorType.Orange) },
            new() { Operator = OperatorType.Etisalat, NameAr = "اتصالات", SimCount = sims.Count(s => s.Operator == OperatorType.Etisalat), TodayOperations = todayTxs.Count(t => t.SimCard?.Operator == OperatorType.Etisalat) },
            new() { Operator = OperatorType.WE, NameAr = "المصرية للاتصالات WE", SimCount = sims.Count(s => s.Operator == OperatorType.WE), TodayOperations = todayTxs.Count(t => t.SimCard?.Operator == OperatorType.WE) }
        };

        var dto = new AdminDashboardDto
        {
            TotalTenants = totalTenants,
            ActiveTenantsCount = activeTenants,
            TotalDevices = totalDevices,
            OnlineDevices = onlineDevices,
            TotalSimCards = totalSims,
            ActiveSimCards = activeSims,
            TodayTransactionsCount = todayCount,
            TodaySuccessfulTransactions = todaySuccess,
            TodayFailedTransactions = todayFailed,
            TotalSimBalance = totalSimBalance,
            TotalPortsCount = totalPorts,
            BusyPortsCount = busyPorts,
            PlatformTotalRevenue = platformRevenue,
            PlatformTotalVolume = platformVolume,
            TopTenants = topTenants,
            CarrierStats = carrierStats,
            Devices = devices,
            RecentTransactions = recentTxs
        };

        return Result.Success(dto);
    }

    public async Task<Result<CompanyDashboardDto>> GetCompanyDashboardDataAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.AsQueryable()
            .Include(t => t.ApiKeys)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
            return Result.Failure<CompanyDashboardDto>("Tenant.NotFound", "الشركة غير موجودة.");

        var today = DateTime.UtcNow.Date;
        var todayOps = await _unitOfWork.Transactions.AsQueryable()
            .Where(t => t.TenantId == tenant.Id && t.RequestedAt >= today)
            .ToListAsync(cancellationToken);

        var assignedDeviceIds = await _unitOfWork.Repository<TenantDevice, int>().AsQueryable()
            .Where(td => td.TenantId == tenant.Id && td.IsActive)
            .Select(td => td.DeviceId)
            .ToListAsync(cancellationToken);

        var sims = await _unitOfWork.SimCards.AsQueryable()
            .Include(s => s.DevicePort)
            .Where(s => !s.IsDelete && assignedDeviceIds.Contains(s.DevicePort.DeviceId))
            .ToListAsync(cancellationToken);

        var recentTxs = await _unitOfWork.Transactions.AsQueryable()
            .Include(t => t.ServiceDefinition)
            .Include(t => t.SimCard)
            .Where(t => t.TenantId == tenant.Id)
            .OrderByDescending(t => t.RequestedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var subscribedServices = await _unitOfWork.Services.AsQueryable()
            .Where(s => s.IsActive && !s.IsDelete && s.TenantSubscriptions.Any(sub => sub.TenantId == tenant.Id && sub.IsActive))
            .ToListAsync(cancellationToken);

        var dto = new CompanyDashboardDto
        {
            Tenant = tenant,
            TodayOperationsCount = todayOps.Count,
            TodaySuccessfulOps = todayOps.Count(t => t.Status == TransactionStatus.Completed),
            AssignedSimCards = sims,
            RecentTransactions = recentTxs,
            AvailableServices = subscribedServices,
            ApiKeys = tenant.ApiKeys.Where(k => k.IsActive).ToList()
        };

        return Result.Success(dto);
    }
}
