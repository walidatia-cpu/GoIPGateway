namespace GoIPGateway.Application.DTOs.Reports;

public class TenantsPerformanceFilterDto
{
    public string? DatePreset { get; set; } = "thismonth"; // today, yesterday, last7days, thismonth, lastmonth, custom
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "volume_desc"; // volume_desc, count_desc, fees_desc, success_rate, name_asc
    public bool? IsActiveOnly { get; set; } = true;
}
