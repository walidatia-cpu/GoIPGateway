using GoIPGateway.Application.DTOs.Dashboard;

namespace GoIPGateway.Web.Models;

public class AdminDashboardViewModel : AdminDashboardDto
{
    public static AdminDashboardViewModel FromDto(AdminDashboardDto dto)
    {
        return new AdminDashboardViewModel
        {
            TotalTenants = dto.TotalTenants,
            ActiveTenantsCount = dto.ActiveTenantsCount,
            TotalDevices = dto.TotalDevices,
            OnlineDevices = dto.OnlineDevices,
            TotalSimCards = dto.TotalSimCards,
            ActiveSimCards = dto.ActiveSimCards,
            TodayTransactionsCount = dto.TodayTransactionsCount,
            TodaySuccessfulTransactions = dto.TodaySuccessfulTransactions,
            TodayFailedTransactions = dto.TodayFailedTransactions,
            TotalSimBalance = dto.TotalSimBalance,
            AverageLatencySeconds = dto.AverageLatencySeconds,
            QueuePendingCount = dto.QueuePendingCount,
            PlatformTotalRevenue = dto.PlatformTotalRevenue,
            PlatformTotalVolume = dto.PlatformTotalVolume,
            TotalPortsCount = dto.TotalPortsCount,
            BusyPortsCount = dto.BusyPortsCount,
            TopTenants = dto.TopTenants,
            CarrierStats = dto.CarrierStats,
            Devices = dto.Devices,
            RecentTransactions = dto.RecentTransactions,
            LowBalanceSims = dto.LowBalanceSims
        };
    }
}

