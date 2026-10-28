namespace GoIPGateway.Application.DTOs.Reports;

public class TenantsPerformanceReportDto
{
    public TenantsPerformanceFilterDto Filter { get; set; } = new();

    // Summary Platform KPIs
    public int TotalTenantsCount { get; set; }
    public int ActiveTenantsCount { get; set; }
    public decimal TotalOverallVolume { get; set; }
    public decimal TotalOverallFees { get; set; }
    public int TotalOverallOperations { get; set; }
    public int TotalSuccessfulOperations { get; set; }
    public int TotalFailedOperations { get; set; }
    public double AverageSuccessRate => TotalOverallOperations > 0
        ? Math.Round((double)TotalSuccessfulOperations / TotalOverallOperations * 100, 1)
        : 100.0;

    // Detailed Companies List
    public List<TenantPerformanceItemDto> Tenants { get; set; } = new();
}
