namespace GoIPGateway.Application.DTOs.Reports;

public class TenantPerformanceItemDto
{
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public decimal CurrentBalance { get; set; }

    // Volume & Financial KPIs
    public decimal TotalFinancialVolume { get; set; }
    public decimal TotalPlatformFees { get; set; }

    // Operations KPIs
    public int TotalOperationsCount { get; set; }
    public int SuccessfulOperationsCount { get; set; }
    public int FailedOperationsCount { get; set; }
    public double SuccessRate => TotalOperationsCount > 0
        ? Math.Round((double)SuccessfulOperationsCount / TotalOperationsCount * 100, 1)
        : 100.0;

    // Infrastructure Assigned
    public int AssignedDevicesCount { get; set; }
    public int ActiveSimsCount { get; set; }

    // Recent activity
    public DateTime? LastOperationAt { get; set; }
    public string? TopServiceName { get; set; }
}
