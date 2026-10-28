using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs.Reports;

public class ServiceReportStatDto
{
    public int ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public ServiceCategory Category { get; set; }
    public OperatorType? Operator { get; set; }
    public int OperationsCount { get; set; }
    public int SuccessfulCount { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalRevenue { get; set; }
    public double SuccessRate => OperationsCount > 0
        ? Math.Round((double)SuccessfulCount / OperationsCount * 100, 1)
        : 100.0;
}
