using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.DTOs.Dashboard;

public class AdminDashboardDto
{
    public int TotalTenants { get; set; }
    public int ActiveTenantsCount { get; set; }
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int TotalSimCards { get; set; }
    public int ActiveSimCards { get; set; }
    public int TodayTransactionsCount { get; set; }
    public int TodaySuccessfulTransactions { get; set; }
    public int TodayFailedTransactions { get; set; }
    public decimal TotalSimBalance { get; set; }
    public double SuccessRate => TodayTransactionsCount > 0
        ? Math.Round((double)TodaySuccessfulTransactions / TodayTransactionsCount * 100, 1)
        : 100.0;

    // Platform & Infrastructure Health Metrics
    public double AverageLatencySeconds { get; set; } = 1.8;
    public int QueuePendingCount { get; set; } = 0;
    public decimal PlatformTotalRevenue { get; set; } = 0;
    public decimal PlatformTotalVolume { get; set; } = 0;
    public int TotalPortsCount { get; set; }
    public int BusyPortsCount { get; set; }

    public List<TenantUsageStatDto> TopTenants { get; set; } = new();
    public List<CarrierStatDto> CarrierStats { get; set; } = new();
    public List<Device> Devices { get; set; } = new();
    public List<TenantTransaction> RecentTransactions { get; set; } = new();
    public List<SimCard> LowBalanceSims { get; set; } = new();
}
