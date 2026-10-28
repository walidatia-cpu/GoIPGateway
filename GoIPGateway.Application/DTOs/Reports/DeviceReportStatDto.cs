using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs.Reports;

public class DeviceReportStatDto
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; }
    public int TotalPorts { get; set; }
    public int ActiveSimsCount { get; set; }
    public int OperationsCount { get; set; }
    public int SuccessfulOperationsCount { get; set; }
    public int FailedOperationsCount { get; set; }
    public decimal TotalVolume { get; set; }
    public double SuccessRate => OperationsCount > 0
        ? Math.Round((double)SuccessfulOperationsCount / OperationsCount * 100, 1)
        : 100.0;
    public List<string> AssociatedSimNumbers { get; set; } = new();
}
