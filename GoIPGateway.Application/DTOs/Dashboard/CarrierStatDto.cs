using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs.Dashboard;

public class CarrierStatDto
{
    public OperatorType Operator { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public int SimCount { get; set; }
    public int TodayOperations { get; set; }
    public double SuccessRate { get; set; } = 100.0;
    public string StatusText { get; set; } = "مستقر";
}
