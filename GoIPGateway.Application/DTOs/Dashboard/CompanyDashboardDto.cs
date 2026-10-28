using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.DTOs.Dashboard;

public class CompanyDashboardDto
{
    public Tenant Tenant { get; set; } = null!;
    public int TodayOperationsCount { get; set; }
    public int TodaySuccessfulOps { get; set; }
    public double SuccessRate => TodayOperationsCount > 0
        ? Math.Round((double)TodaySuccessfulOps / TodayOperationsCount * 100, 1)
        : 100.0;

    public List<SimCard> AssignedSimCards { get; set; } = new();
    public List<TenantTransaction> RecentTransactions { get; set; } = new();
    public List<ServiceDefinition> AvailableServices { get; set; } = new();
    public List<TenantApiKey> ApiKeys { get; set; } = new();
}
