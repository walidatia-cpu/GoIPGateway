using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Web.Models;

public class AdminDashboardViewModel
{
    public int TotalTenants { get; set; }
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

    public List<Device> Devices { get; set; } = new();
    public List<TenantTransaction> RecentTransactions { get; set; } = new();
    public List<SimCard> LowBalanceSims { get; set; } = new();
}

public class CompanyDashboardViewModel
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

public class ExecuteServiceViewModel
{
    public string ServiceCode { get; set; } = string.Empty;
    public int? SimCardId { get; set; }
    public OperatorType? Operator { get; set; }
    public string? ExternalRef { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();

    // Catalog for UI
    public List<ServiceDefinition> Services { get; set; } = new();
    public List<SimCard> AvailableSims { get; set; } = new();
}

public class LoginViewModel
{
    public string Email { get; set; } = "admin@goipgateway.com";
    public string Password { get; set; } = "Admin@123456";
    public bool RememberMe { get; set; } = true;
    public string? ReturnUrl { get; set; }
}
