using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Dashboard;
using GoIPGateway.Application.DTOs.Reports;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.Reports;

public class ReportQueryService : IReportQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CompanyReportDto>> GetCompanyReportAsync(CompanyReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        // 1. Resolve Date Range according to preset or custom dates
        ResolveDateRange(filter);

        var report = new CompanyReportDto
        {
            Filter = filter
        };

        // 2. Load available Tenants for the dropdown
        report.AvailableTenants = await _unitOfWork.Tenants.AsQueryable()
            .Where(t => !t.IsDelete)
            .OrderBy(t => t.CompanyName)
            .ToListAsync(cancellationToken);

        if (filter.TenantId.HasValue)
        {
            report.SelectedTenant = report.AvailableTenants.FirstOrDefault(t => t.Id == filter.TenantId.Value);
        }

        // 3. Load available Devices for the filter (restricted to tenant if selected)
        var devicesQuery = _unitOfWork.Devices.AsQueryable()
            .Include(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .Where(d => !d.IsDelete);

        if (filter.TenantId.HasValue)
        {
            var assignedDeviceIds = await _unitOfWork.Repository<TenantDevice, int>().AsQueryable()
                .Where(td => td.TenantId == filter.TenantId.Value && td.IsActive)
                .Select(td => td.DeviceId)
                .ToListAsync(cancellationToken);

            devicesQuery = devicesQuery.Where(d => assignedDeviceIds.Contains(d.Id));
        }

        report.AvailableDevices = await devicesQuery.OrderBy(d => d.DeviceName).ToListAsync(cancellationToken);

        // 4. Load available SIM Cards (restricted to tenant devices if selected)
        var devIds = report.AvailableDevices.Select(d => d.Id).ToHashSet();
        report.AvailableSimCards = await _unitOfWork.SimCards.AsQueryable()
            .Include(s => s.DevicePort)
                .ThenInclude(p => p.Device)
            .Where(s => !s.IsDelete && devIds.Contains(s.DevicePort.DeviceId))
            .OrderBy(s => s.PhoneNumber)
            .ToListAsync(cancellationToken);

        // 5. Load available Services
        report.AvailableServices = await _unitOfWork.Services.AsQueryable()
            .Where(s => !s.IsDelete && s.IsActive)
            .OrderBy(s => s.NameAr)
            .ToListAsync(cancellationToken);

        // 6. Base Transactions Query with comprehensive filters
        var txQuery = _unitOfWork.Transactions.AsQueryable()
            .Include(t => t.Tenant)
            .Include(t => t.ServiceDefinition)
            .Include(t => t.SimCard)
            .Include(t => t.DevicePort)
                .ThenInclude(p => p!.Device)
            .AsQueryable();

        if (filter.TenantId.HasValue)
        {
            txQuery = txQuery.Where(t => t.TenantId == filter.TenantId.Value);
        }

        if (filter.DeviceId.HasValue)
        {
            txQuery = txQuery.Where(t => t.DevicePort != null && t.DevicePort.DeviceId == filter.DeviceId.Value);
        }

        if (filter.SimCardId.HasValue)
        {
            txQuery = txQuery.Where(t => t.SimCardId == filter.SimCardId.Value);
        }

        if (filter.Operator.HasValue)
        {
            txQuery = txQuery.Where(t => (t.ServiceDefinition != null && t.ServiceDefinition.Operator == filter.Operator.Value) ||
                                         (t.SimCard != null && t.SimCard.Operator == filter.Operator.Value));
        }

        if (filter.ServiceDefinitionId.HasValue)
        {
            txQuery = txQuery.Where(t => t.ServiceDefinitionId == filter.ServiceDefinitionId.Value);
        }

        if (filter.Status.HasValue)
        {
            txQuery = txQuery.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            txQuery = txQuery.Where(t => t.RequestedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            txQuery = txQuery.Where(t => t.RequestedAt <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            txQuery = txQuery.Where(t => t.TransactionRef.ToLower().Contains(search) ||
                                         (t.ExternalRef != null && t.ExternalRef.ToLower().Contains(search)) ||
                                         (t.SimCard != null && t.SimCard.PhoneNumber != null && t.SimCard.PhoneNumber.Contains(search)));
        }

        // 7. Aggregate KPI Metrics across all filtered transactions
        var allFilteredTxs = await txQuery.ToListAsync(cancellationToken);

        report.TotalTransactionsCount = allFilteredTxs.Count;
        report.SuccessfulTransactionsCount = allFilteredTxs.Count(t => t.Status == TransactionStatus.Completed);
        report.FailedTransactionsCount = allFilteredTxs.Count(t => t.Status == TransactionStatus.Failed);
        report.TotalFinancialVolume = allFilteredTxs.Sum(t => t.Amount ?? 0);
        report.TotalPlatformFees = allFilteredTxs.Sum(t => t.ServiceCost);

        report.TotalActiveDevices = report.AvailableDevices.Count(d => d.IsActive && !d.IsDelete);
        report.TotalActiveSims = report.AvailableSimCards.Count(s => s.IsActive && !s.IsDelete && !s.IsBlocked);
        report.TotalSimsBalance = report.AvailableSimCards.Where(s => s.IsActive).Sum(s => s.CurrentBalance);

        // 8. "وتاخد الاجهزه معاك" - Detailed Device Breakdown
        foreach (var device in report.AvailableDevices)
        {
            var deviceTxs = allFilteredTxs.Where(t => t.DevicePort != null && t.DevicePort.DeviceId == device.Id).ToList();
            var deviceSims = report.AvailableSimCards.Where(s => s.DevicePort?.DeviceId == device.Id).ToList();

            report.DeviceStats.Add(new DeviceReportStatDto
            {
                DeviceId = device.Id,
                DeviceName = device.DeviceName,
                IpAddress = device.IpAddress,
                DeviceType = device.DeviceType,
                IsOnline = device.IsOnline,
                IsActive = device.IsActive,
                TotalPorts = device.Ports.Count > 0 ? device.Ports.Count : device.TotalPorts,
                ActiveSimsCount = deviceSims.Count(s => s.IsActive),
                OperationsCount = deviceTxs.Count,
                SuccessfulOperationsCount = deviceTxs.Count(t => t.Status == TransactionStatus.Completed),
                FailedOperationsCount = deviceTxs.Count(t => t.Status == TransactionStatus.Failed),
                TotalVolume = deviceTxs.Sum(t => t.Amount ?? 0),
                AssociatedSimNumbers = deviceSims.Select(s => s.PhoneNumber ?? s.SimNumber).Where(n => !string.IsNullOrEmpty(n)).ToList()!
            });
        }

        // 9. Carrier Breakdown
        var carrierOps = new[]
        {
            (Op: OperatorType.Vodafone, Name: "فودافون كاش"),
            (Op: OperatorType.Orange, Name: "أورانج كاش"),
            (Op: OperatorType.Etisalat, Name: "اتصالات كاش"),
            (Op: OperatorType.WE, Name: "وي باي (WE)")
        };

        foreach (var c in carrierOps)
        {
            var opTxs = allFilteredTxs.Where(t => t.ServiceDefinition?.Operator == c.Op || t.SimCard?.Operator == c.Op).ToList();
            var simCount = report.AvailableSimCards.Count(s => s.Operator == c.Op);
            var successRate = opTxs.Any()
                ? Math.Round((double)opTxs.Count(t => t.Status == TransactionStatus.Completed) / opTxs.Count * 100, 1)
                : 100.0;

            report.CarrierStats.Add(new CarrierStatDto
            {
                Operator = c.Op,
                NameAr = c.Name,
                SimCount = simCount,
                TodayOperations = opTxs.Count,
                SuccessRate = successRate,
                StatusText = successRate >= 90 ? "ممتاز" : (successRate >= 75 ? "مستقر" : "يحتاج متابعة")
            });
        }

        // 10. Service Breakdown
        var serviceGroups = allFilteredTxs.GroupBy(t => t.ServiceDefinitionId);
        foreach (var grp in serviceGroups)
        {
            var svc = report.AvailableServices.FirstOrDefault(s => s.Id == grp.Key);
            if (svc != null)
            {
                var txs = grp.ToList();
                report.ServiceStats.Add(new ServiceReportStatDto
                {
                    ServiceId = svc.Id,
                    ServiceCode = svc.ServiceCode,
                    NameAr = svc.NameAr,
                    Category = svc.Category,
                    Operator = svc.Operator,
                    OperationsCount = txs.Count,
                    SuccessfulCount = txs.Count(t => t.Status == TransactionStatus.Completed),
                    TotalVolume = txs.Sum(t => t.Amount ?? 0),
                    TotalRevenue = txs.Sum(t => t.ServiceCost)
                });
            }
        }
        report.ServiceStats = report.ServiceStats.OrderByDescending(s => s.OperationsCount).ToList();

        // 11. Paged Transactions List
        var page = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 25;
        var pagedItems = allFilteredTxs
            .OrderByDescending(t => t.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        report.Transactions = new PagedResult<TenantTransaction>(pagedItems, report.TotalTransactionsCount, page, pageSize);

        return Result.Success(report);
    }

    private static void ResolveDateRange(CompanyReportFilterDto filter)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        if (string.IsNullOrWhiteSpace(filter.DatePreset))
        {
            filter.DatePreset = "thismonth";
        }

        switch (filter.DatePreset.ToLowerInvariant())
        {
            case "today":
                filter.FromDate = today;
                filter.ToDate = today.AddDays(1).AddTicks(-1);
                break;
            case "yesterday":
                var yesterday = today.AddDays(-1);
                filter.FromDate = yesterday;
                filter.ToDate = today.AddTicks(-1);
                break;
            case "last7days":
                filter.FromDate = today.AddDays(-7);
                filter.ToDate = now;
                break;
            case "thismonth":
                filter.FromDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                filter.ToDate = now;
                break;
            case "lastmonth":
                var lastMonthDate = today.AddMonths(-1);
                var firstDayLastMonth = new DateTime(lastMonthDate.Year, lastMonthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var lastDayLastMonth = firstDayLastMonth.AddMonths(1).AddTicks(-1);
                filter.FromDate = firstDayLastMonth;
                filter.ToDate = lastDayLastMonth;
                break;
            case "custom":
                // Preserve manually selected FromDate and ToDate
                if (!filter.FromDate.HasValue) filter.FromDate = today.AddDays(-30);
                if (!filter.ToDate.HasValue) filter.ToDate = now;
                break;
            default:
                if (!filter.FromDate.HasValue) filter.FromDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                if (!filter.ToDate.HasValue) filter.ToDate = now;
                break;
        }
    }

    public async Task<Result<TenantsPerformanceReportDto>> GetTenantsPerformanceReportAsync(TenantsPerformanceFilterDto filter, CancellationToken cancellationToken = default)
    {
        NormalizeTenantsFilterDates(filter);

        var tenantsQuery = _unitOfWork.Tenants.AsQueryable()
            .Include(t => t.AssignedDevices)
                .ThenInclude(ad => ad.Device)
                .ThenInclude(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .Where(t => !t.IsDelete);

        if (filter.IsActiveOnly == true)
        {
            tenantsQuery = tenantsQuery.Where(t => t.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            tenantsQuery = tenantsQuery.Where(t => t.CompanyName.ToLower().Contains(search)
                || (t.ContactPerson != null && t.ContactPerson.ToLower().Contains(search))
                || (t.ContactEmail != null && t.ContactEmail.ToLower().Contains(search))
                || (t.ContactPhone != null && t.ContactPhone.Contains(search)));
        }

        var tenants = await tenantsQuery.ToListAsync(cancellationToken);

        // Fetch transactions within the filtered date range
        var txQuery = _unitOfWork.Transactions.AsQueryable()
            .Include(t => t.ServiceDefinition)
            .Where(t => t.RequestedAt >= filter.FromDate!.Value && t.RequestedAt <= filter.ToDate!.Value);

        var allTransactions = await txQuery.ToListAsync(cancellationToken);
        var txByTenant = allTransactions.GroupBy(t => t.TenantId).ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<TenantPerformanceItemDto>();

        foreach (var tenant in tenants)
        {
            var tenantTxs = txByTenant.TryGetValue(tenant.Id, out var txs) ? txs : new List<TenantTransaction>();

            var totalOps = tenantTxs.Count;
            var successfulOps = tenantTxs.Count(t => t.Status == TransactionStatus.Completed);
            var failedOps = tenantTxs.Count(t => t.Status == TransactionStatus.Failed);
            var totalVolume = tenantTxs.Where(t => t.Status == TransactionStatus.Completed && t.Amount.HasValue).Sum(t => t.Amount!.Value);
            var totalFees = tenantTxs.Sum(t => t.TenantCharge);

            var activeDevices = tenant.AssignedDevices.Where(ad => ad.IsActive && ad.Device != null).Select(ad => ad.Device).Distinct().ToList();
            var activeSimsCount = activeDevices.SelectMany(d => d.Ports).Count(p => p.SimCard != null && p.SimCard.IsActive && !p.SimCard.IsDelete);

            var lastOp = tenantTxs.OrderByDescending(t => t.RequestedAt).FirstOrDefault()?.RequestedAt;
            var topService = tenantTxs.Where(t => t.ServiceDefinition != null)
                .GroupBy(t => t.ServiceDefinition!.NameAr)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            items.Add(new TenantPerformanceItemDto
            {
                TenantId = tenant.Id,
                CompanyName = tenant.CompanyName,
                ContactPerson = tenant.ContactPerson,
                ContactEmail = tenant.ContactEmail,
                ContactPhone = tenant.ContactPhone,
                IsActive = tenant.IsActive,
                IsBlocked = tenant.IsBlocked,
                CurrentBalance = tenant.Balance,
                TotalFinancialVolume = totalVolume,
                TotalPlatformFees = totalFees,
                TotalOperationsCount = totalOps,
                SuccessfulOperationsCount = successfulOps,
                FailedOperationsCount = failedOps,
                AssignedDevicesCount = activeDevices.Count,
                ActiveSimsCount = activeSimsCount,
                LastOperationAt = lastOp,
                TopServiceName = topService
            });
        }

        // Apply sorting
        items = (filter.SortBy?.ToLowerInvariant()) switch
        {
            "count_desc" => items.OrderByDescending(x => x.TotalOperationsCount).ThenByDescending(x => x.TotalFinancialVolume).ToList(),
            "fees_desc" => items.OrderByDescending(x => x.TotalPlatformFees).ThenByDescending(x => x.TotalFinancialVolume).ToList(),
            "success_rate" => items.OrderByDescending(x => x.SuccessRate).ThenByDescending(x => x.TotalOperationsCount).ToList(),
            "name_asc" => items.OrderBy(x => x.CompanyName).ToList(),
            _ => items.OrderByDescending(x => x.TotalFinancialVolume).ThenByDescending(x => x.TotalOperationsCount).ToList()
        };

        var report = new TenantsPerformanceReportDto
        {
            Filter = filter,
            TotalTenantsCount = tenants.Count,
            ActiveTenantsCount = tenants.Count(t => t.IsActive && !t.IsBlocked),
            TotalOverallVolume = items.Sum(x => x.TotalFinancialVolume),
            TotalOverallFees = items.Sum(x => x.TotalPlatformFees),
            TotalOverallOperations = items.Sum(x => x.TotalOperationsCount),
            TotalSuccessfulOperations = items.Sum(x => x.SuccessfulOperationsCount),
            TotalFailedOperations = items.Sum(x => x.FailedOperationsCount),
            Tenants = items
        };

        return Result.Success(report);
    }

    private void NormalizeTenantsFilterDates(TenantsPerformanceFilterDto filter)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        if (string.IsNullOrWhiteSpace(filter.DatePreset))
        {
            filter.DatePreset = "thismonth";
        }

        switch (filter.DatePreset.ToLowerInvariant())
        {
            case "today":
                filter.FromDate = today;
                filter.ToDate = today.AddDays(1).AddTicks(-1);
                break;
            case "yesterday":
                var yesterday = today.AddDays(-1);
                filter.FromDate = yesterday;
                filter.ToDate = today.AddTicks(-1);
                break;
            case "last7days":
                filter.FromDate = today.AddDays(-7);
                filter.ToDate = now;
                break;
            case "thismonth":
                filter.FromDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                filter.ToDate = now;
                break;
            case "lastmonth":
                var lastMonthDate = today.AddMonths(-1);
                var firstDayLastMonth = new DateTime(lastMonthDate.Year, lastMonthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var lastDayLastMonth = firstDayLastMonth.AddMonths(1).AddTicks(-1);
                filter.FromDate = firstDayLastMonth;
                filter.ToDate = lastDayLastMonth;
                break;
            case "custom":
                if (!filter.FromDate.HasValue) filter.FromDate = today.AddDays(-30);
                if (!filter.ToDate.HasValue) filter.ToDate = now;
                break;
            default:
                if (!filter.FromDate.HasValue) filter.FromDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                if (!filter.ToDate.HasValue) filter.ToDate = now;
                break;
        }
    }

    public async Task<Result<ExecutiveSummaryDto>> GetExecutiveSummaryReportAsync(ExecutiveSummaryFilterDto filter, CancellationToken cancellationToken = default)
    {
        NormalizeExecutiveSummaryFilterDates(filter);

        string scopeTitle = "التقرير الإجمالي للمنصة المركزية";
        bool isAdminScope = true;

        if (filter.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(filter.TenantId.Value, cancellationToken);
            if (tenant != null)
            {
                scopeTitle = $"التقرير الإجمالي لشركة {tenant.CompanyName}";
                isAdminScope = false;
            }
        }

        // Available Tenants for Admin dropdown
        var availableTenants = await _unitOfWork.Tenants.AsQueryable()
            .Where(t => !t.IsDelete)
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantLookupDto { Id = t.Id, CompanyName = t.CompanyName })
            .ToListAsync(cancellationToken);

        // Transactions Query
        var txQuery = _unitOfWork.Transactions.AsQueryable()
            .Include(t => t.ServiceDefinition)
            .Include(t => t.Tenant)
            .Include(t => t.SimCard)
            .Where(t => t.RequestedAt >= filter.FromDate!.Value && t.RequestedAt <= filter.ToDate!.Value);

        if (filter.TenantId.HasValue)
        {
            txQuery = txQuery.Where(t => t.TenantId == filter.TenantId.Value);
        }

        if (filter.Operator.HasValue)
        {
            txQuery = txQuery.Where(t => (t.ServiceDefinition != null && t.ServiceDefinition.Operator == filter.Operator.Value)
                || (t.SimCard != null && t.SimCard.Operator == filter.Operator.Value));
        }

        var transactions = await txQuery.ToListAsync(cancellationToken);

        // Financial Aggregates
        var totalOps = transactions.Count;
        var completedOps = transactions.Count(t => t.Status == TransactionStatus.Completed);
        var failedOps = transactions.Count(t => t.Status == TransactionStatus.Failed);
        var processingOps = transactions.Count(t => t.Status == TransactionStatus.Processing);
        var pendingOps = transactions.Count(t => t.Status == TransactionStatus.Pending);

        var totalGrossVolume = transactions.Where(t => t.Amount.HasValue).Sum(t => t.Amount!.Value);
        var totalSuccessfulVolume = transactions.Where(t => t.Status == TransactionStatus.Completed && t.Amount.HasValue).Sum(t => t.Amount!.Value);
        var totalFailedVolume = transactions.Where(t => t.Status == TransactionStatus.Failed && t.Amount.HasValue).Sum(t => t.Amount!.Value);
        var totalFees = transactions.Sum(t => t.TenantCharge);

        // Hardware & SIMs Aggregates
        var devicesQuery = _unitOfWork.Devices.AsQueryable()
            .Include(d => d.Ports)
                .ThenInclude(p => p.SimCard)
            .Where(d => !d.IsDelete);

        if (filter.TenantId.HasValue)
        {
            var assignedDeviceIds = await _unitOfWork.Repository<TenantDevice, int>().AsQueryable()
                .Where(td => td.TenantId == filter.TenantId.Value && td.IsActive)
                .Select(td => td.DeviceId)
                .ToListAsync(cancellationToken);

            devicesQuery = devicesQuery.Where(d => assignedDeviceIds.Contains(d.Id));
        }

        var devices = await devicesQuery.ToListAsync(cancellationToken);
        var totalDevices = devices.Count;
        var onlineDevices = devices.Count(d => d.IsOnline && d.IsActive);
        var allPorts = devices.SelectMany(d => d.Ports).ToList();
        var totalPorts = allPorts.Count;
        var allSims = allPorts.Where(p => p.SimCard != null && !p.SimCard.IsDelete).Select(p => p.SimCard!).ToList();
        var activeSims = allSims.Count(s => s.IsActive && !s.IsBlocked);
        var blockedSims = allSims.Count(s => s.IsBlocked);
        var totalLiquidity = allSims.Sum(s => s.CurrentBalance);

        // Accounts Balance
        decimal totalAccountsBalance = 0;
        if (filter.TenantId.HasValue)
        {
            var t = await _unitOfWork.Tenants.GetByIdAsync(filter.TenantId.Value, cancellationToken);
            totalAccountsBalance = t?.Balance ?? 0;
        }
        else
        {
            totalAccountsBalance = await _unitOfWork.Tenants.AsQueryable()
                .Where(t => !t.IsDelete)
                .SumAsync(t => t.Balance, cancellationToken);
        }

        // Operator Aggregates
        var operatorAggregates = new List<OperatorAggregateDto>();
        foreach (OperatorType op in Enum.GetValues<OperatorType>())
        {
            var opName = op switch
            {
                OperatorType.Vodafone => "فودافون كاش",
                OperatorType.Orange => "أورانج كاش",
                OperatorType.Etisalat => "اتصالات كاش",
                OperatorType.WE => "وي باي (WE)",
                _ => op.ToString()
            };
            var opColor = op switch
            {
                OperatorType.Vodafone => "#e60000",
                OperatorType.Orange => "#ff7900",
                OperatorType.Etisalat => "#78be20",
                OperatorType.WE => "#5c2d91",
                _ => "#2563eb"
            };

            var opSims = allSims.Where(s => s.Operator == op).ToList();
            var opLiquidity = opSims.Sum(s => s.CurrentBalance);

            var opTxs = transactions.Where(t => (t.ServiceDefinition != null && t.ServiceDefinition.Operator == op)
                || (t.SimCard != null && t.SimCard.Operator == op)).ToList();

            var opTotalOps = opTxs.Count;
            var opSuccessOps = opTxs.Count(t => t.Status == TransactionStatus.Completed);
            var opVolume = opTxs.Where(t => t.Status == TransactionStatus.Completed && t.Amount.HasValue).Sum(t => t.Amount!.Value);
            var share = totalSuccessfulVolume > 0 ? Math.Round((double)opVolume / (double)totalSuccessfulVolume * 100, 1) : 0;

            operatorAggregates.Add(new OperatorAggregateDto
            {
                Operator = op,
                NameAr = opName,
                ColorHex = opColor,
                SimCount = opSims.Count,
                CurrentLiquidity = opLiquidity,
                TotalOperations = opTotalOps,
                SuccessfulOperations = opSuccessOps,
                TotalVolume = opVolume,
                SharePercentage = share
            });
        }

        // Service Category Aggregates
        var serviceAggregates = transactions
            .Where(t => t.ServiceDefinition != null)
            .GroupBy(t => t.ServiceDefinition!.Category)
            .Select(g => new ServiceCategoryAggregateDto
            {
                Category = g.Key,
                NameAr = g.Key switch
                {
                    ServiceCategory.Payment => "عمليات الدفع والتحصيل",
                    ServiceCategory.Transfer => "تحويل الأموال بين المحافظ",
                    ServiceCategory.Settlement => "التسويات المالية",
                    ServiceCategory.Inquiry => "الاستعلام عن الرصيد",
                    ServiceCategory.NumberInquiry => "استعلام أرقام الشرائح",
                    ServiceCategory.Management => "إدارة وضبط الخدمات",
                    _ => g.Key.ToString()
                },
                TotalOperations = g.Count(),
                TotalVolume = g.Where(t => t.Status == TransactionStatus.Completed && t.Amount.HasValue).Sum(t => t.Amount!.Value),
                TotalRevenue = g.Sum(t => t.TenantCharge),
                SuccessRate = g.Any() ? Math.Round((double)g.Count(t => t.Status == TransactionStatus.Completed) / g.Count() * 100, 1) : 100.0
            })
            .OrderByDescending(s => s.TotalVolume)
            .ToList();

        // Daily Trends
        var dailyTrends = transactions
            .GroupBy(t => t.RequestedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyVolumeAggregateDto
            {
                Date = g.Key,
                DateDisplay = g.Key.ToString("yyyy-MM-dd"),
                TotalOperations = g.Count(),
                SuccessfulOperations = g.Count(t => t.Status == TransactionStatus.Completed),
                TotalVolume = g.Where(t => t.Status == TransactionStatus.Completed && t.Amount.HasValue).Sum(t => t.Amount!.Value),
                TotalFees = g.Sum(t => t.TenantCharge)
            })
            .ToList();

        // Failure Reasons
        var failedTxs = transactions.Where(t => t.Status == TransactionStatus.Failed).ToList();
        var failureReasons = failedTxs
            .GroupBy(t => !string.IsNullOrWhiteSpace(t.ErrorMessage) ? t.ErrorMessage.Trim() : (!string.IsNullOrWhiteSpace(t.RawResponse) ? t.RawResponse.Trim() : "خطأ غير محدد"))
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new FailureReasonAggregateDto
            {
                Reason = g.Key,
                Count = g.Count(),
                Percentage = failedTxs.Count > 0 ? Math.Round((double)g.Count() / failedTxs.Count * 100, 1) : 0
            })
            .ToList();

        // Top Tenants (if Admin)
        var topTenants = new List<TopTenantAggregateDto>();
        if (!filter.TenantId.HasValue)
        {
            topTenants = transactions
                .Where(t => t.Tenant != null)
                .GroupBy(t => new { t.TenantId, t.Tenant!.CompanyName })
                .Select(g => new TopTenantAggregateDto
                {
                    TenantId = g.Key.TenantId,
                    CompanyName = g.Key.CompanyName,
                    TotalOperations = g.Count(),
                    TotalVolume = g.Where(t => t.Status == TransactionStatus.Completed && t.Amount.HasValue).Sum(t => t.Amount!.Value),
                    TotalFees = g.Sum(t => t.TenantCharge),
                    SuccessRate = g.Any() ? Math.Round((double)g.Count(t => t.Status == TransactionStatus.Completed) / g.Count() * 100, 1) : 100.0,
                    SharePercentage = totalSuccessfulVolume > 0 ? Math.Round((double)g.Where(t => t.Status == TransactionStatus.Completed && t.Amount.HasValue).Sum(t => t.Amount!.Value) / (double)totalSuccessfulVolume * 100, 1) : 0
                })
                .OrderByDescending(t => t.TotalVolume)
                .Take(5)
                .ToList();
        }

        var dto = new ExecutiveSummaryDto
        {
            Filter = filter,
            ScopeTitle = scopeTitle,
            IsAdminScope = isAdminScope,
            TotalGrossVolume = totalGrossVolume,
            TotalSuccessfulVolume = totalSuccessfulVolume,
            TotalFailedVolume = totalFailedVolume,
            TotalPlatformFees = totalFees,
            TotalWalletsLiquidity = totalLiquidity,
            TotalAccountsBalance = totalAccountsBalance,
            TotalOperationsCount = totalOps,
            CompletedCount = completedOps,
            FailedCount = failedOps,
            ProcessingCount = processingOps,
            PendingCount = pendingOps,
            TotalDevices = totalDevices,
            OnlineDevices = onlineDevices,
            TotalPorts = totalPorts,
            ActiveSims = activeSims,
            BlockedSims = blockedSims,
            OperatorAggregates = operatorAggregates,
            ServiceAggregates = serviceAggregates,
            DailyTrends = dailyTrends,
            FailureReasons = failureReasons,
            TopTenants = topTenants,
            AvailableTenants = availableTenants
        };

        return Result.Success(dto);
    }

    private void NormalizeExecutiveSummaryFilterDates(ExecutiveSummaryFilterDto filter)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        if (string.IsNullOrWhiteSpace(filter.DatePreset))
        {
            filter.DatePreset = "thismonth";
        }

        switch (filter.DatePreset.ToLowerInvariant())
        {
            case "today":
                filter.FromDate = today;
                filter.ToDate = today.AddDays(1).AddTicks(-1);
                break;
            case "yesterday":
                var yesterday = today.AddDays(-1);
                filter.FromDate = yesterday;
                filter.ToDate = today.AddTicks(-1);
                break;
            case "last7days":
                filter.FromDate = today.AddDays(-7);
                filter.ToDate = now;
                break;
            case "thismonth":
                filter.FromDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                filter.ToDate = now;
                break;
            case "lastmonth":
                var lastMonthDate = today.AddMonths(-1);
                var firstDayLastMonth = new DateTime(lastMonthDate.Year, lastMonthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var lastDayLastMonth = firstDayLastMonth.AddMonths(1).AddTicks(-1);
                filter.FromDate = firstDayLastMonth;
                filter.ToDate = lastDayLastMonth;
                break;
            case "thisyear":
                filter.FromDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                filter.ToDate = now;
                break;
            case "custom":
                if (!filter.FromDate.HasValue) filter.FromDate = today.AddDays(-30);
                if (!filter.ToDate.HasValue) filter.ToDate = now;
                break;
            default:
                if (!filter.FromDate.HasValue) filter.FromDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                if (!filter.ToDate.HasValue) filter.ToDate = now;
                break;
        }
    }
}
