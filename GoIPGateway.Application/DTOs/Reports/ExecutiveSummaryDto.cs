using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs.Reports;

public class ExecutiveSummaryDto
{
    public ExecutiveSummaryFilterDto Filter { get; set; } = new();
    public string ScopeTitle { get; set; } = "التقرير الإجمالي للمنصة المركزية";
    public bool IsAdminScope { get; set; } = true;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // 1. Overall Financial Aggregates (الإجماليات المالية)
    public decimal TotalGrossVolume { get; set; }
    public decimal TotalSuccessfulVolume { get; set; }
    public decimal TotalFailedVolume { get; set; }
    public decimal TotalPlatformFees { get; set; }
    public decimal NetSettledVolume => TotalSuccessfulVolume - TotalPlatformFees;
    public decimal TotalWalletsLiquidity { get; set; }
    public decimal TotalAccountsBalance { get; set; }

    // 2. Transaction Status Aggregates (إجماليات الحالات ومعدلات الإنجاز)
    public int TotalOperationsCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int PendingCount { get; set; }
    public double SuccessRate => TotalOperationsCount > 0
        ? Math.Round((double)CompletedCount / TotalOperationsCount * 100, 1)
        : 100.0;
    public double FailureRate => TotalOperationsCount > 0
        ? Math.Round((double)FailedCount / TotalOperationsCount * 100, 1)
        : 0.0;

    // Average transaction size
    public decimal AverageTransactionSize => CompletedCount > 0
        ? Math.Round(TotalSuccessfulVolume / CompletedCount, 2)
        : 0;

    // 3. Hardware Fleet Aggregates (إجماليات البوابات والخطوط)
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int OfflineDevices => TotalDevices - OnlineDevices;
    public int TotalPorts { get; set; }
    public int ActiveSims { get; set; }
    public int BlockedSims { get; set; }

    // 4. Operator / Carrier Distribution (إجماليات الشبكات والمحافظ)
    public List<OperatorAggregateDto> OperatorAggregates { get; set; } = new();

    // 5. Service Category Distribution (إجماليات فئات الخدمات)
    public List<ServiceCategoryAggregateDto> ServiceAggregates { get; set; } = new();

    // 6. Daily Trend Aggregation (التوزيع الزمني التراكمي لليوميات)
    public List<DailyVolumeAggregateDto> DailyTrends { get; set; } = new();

    // 7. Top Failure Reasons (تحليل أسباب الفشل والتعثر)
    public List<FailureReasonAggregateDto> FailureReasons { get; set; } = new();

    // 8. If Admin: Top Performing Companies vs Overall (أعلى الشركات في حجم العمليات)
    public List<TopTenantAggregateDto> TopTenants { get; set; } = new();

    // Available Tenants Lookup for Admin Filter
    public List<TenantLookupDto> AvailableTenants { get; set; } = new();
}

public class OperatorAggregateDto
{
    public OperatorType Operator { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#2563eb";
    public int SimCount { get; set; }
    public decimal CurrentLiquidity { get; set; }
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public decimal TotalVolume { get; set; }
    public double SuccessRate => TotalOperations > 0
        ? Math.Round((double)SuccessfulOperations / TotalOperations * 100, 1)
        : 100.0;
    public double SharePercentage { get; set; }
}

public class ServiceCategoryAggregateDto
{
    public ServiceCategory Category { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalRevenue { get; set; }
    public double SuccessRate { get; set; }
}

public class DailyVolumeAggregateDto
{
    public DateTime Date { get; set; }
    public string DateDisplay { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalFees { get; set; }
    public double SuccessRate => TotalOperations > 0
        ? Math.Round((double)SuccessfulOperations / TotalOperations * 100, 1)
        : 100.0;
}

public class FailureReasonAggregateDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class TopTenantAggregateDto
{
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalFees { get; set; }
    public double SuccessRate { get; set; }
    public double SharePercentage { get; set; }
}

public class TenantLookupDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
