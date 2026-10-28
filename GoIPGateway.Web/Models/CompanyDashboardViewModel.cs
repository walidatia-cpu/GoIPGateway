using GoIPGateway.Application.DTOs.Dashboard;

namespace GoIPGateway.Web.Models;

public class CompanyDashboardViewModel : CompanyDashboardDto
{
    public static CompanyDashboardViewModel FromDto(CompanyDashboardDto dto)
    {
        return new CompanyDashboardViewModel
        {
            Tenant = dto.Tenant,
            TodayOperationsCount = dto.TodayOperationsCount,
            TodaySuccessfulOps = dto.TodaySuccessfulOps,
            AssignedSimCards = dto.AssignedSimCards,
            RecentTransactions = dto.RecentTransactions,
            AvailableServices = dto.AvailableServices,
            ApiKeys = dto.ApiKeys
        };
    }
}

