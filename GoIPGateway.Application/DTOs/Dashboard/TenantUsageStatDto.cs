namespace GoIPGateway.Application.DTOs.Dashboard;

public class TenantUsageStatDto
{
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int ActiveApiKeysCount { get; set; }
    public int TodayTransactionsCount { get; set; }
    public int SuccessfulTransactionsCount { get; set; }
    public int FailedTransactionsCount { get; set; }
    public double SuccessRate => TodayTransactionsCount > 0
        ? Math.Round((double)SuccessfulTransactionsCount / TodayTransactionsCount * 100, 1)
        : 100.0;
    public decimal TotalVolume { get; set; }
}
