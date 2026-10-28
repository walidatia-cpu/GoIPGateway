using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Dashboard;
using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.DTOs.Reports;

public class CompanyReportDto
{
    // Filter applied
    public CompanyReportFilterDto Filter { get; set; } = new();

    // Tenant info (if single tenant filtered)
    public Tenant? SelectedTenant { get; set; }
    public List<Tenant> AvailableTenants { get; set; } = new();
    public List<Device> AvailableDevices { get; set; } = new();
    public List<SimCard> AvailableSimCards { get; set; } = new();
    public List<ServiceDefinition> AvailableServices { get; set; } = new();

    // Summary KPIs
    public int TotalTransactionsCount { get; set; }
    public int SuccessfulTransactionsCount { get; set; }
    public int FailedTransactionsCount { get; set; }
    public double SuccessRate => TotalTransactionsCount > 0
        ? Math.Round((double)SuccessfulTransactionsCount / TotalTransactionsCount * 100, 1)
        : 100.0;

    public decimal TotalFinancialVolume { get; set; }
    public decimal TotalPlatformFees { get; set; }
    public int TotalActiveDevices { get; set; }
    public int TotalActiveSims { get; set; }
    public decimal TotalSimsBalance { get; set; }

    // Device Breakdown ("وتاخد الاجهزه معاك")
    public List<DeviceReportStatDto> DeviceStats { get; set; } = new();

    // Carrier Breakdown
    public List<CarrierStatDto> CarrierStats { get; set; } = new();

    // Service Breakdown
    public List<ServiceReportStatDto> ServiceStats { get; set; } = new();

    // Detailed Filtered Transactions List
    public PagedResult<TenantTransaction> Transactions { get; set; } = new(new List<TenantTransaction>(), 0, 1, 25);
}
