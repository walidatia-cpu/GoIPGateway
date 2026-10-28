using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs.Reports;

public class ExecutiveSummaryFilterDto
{
    public int? TenantId { get; set; }
    public string DatePreset { get; set; } = "thismonth"; // today, yesterday, last7days, thismonth, lastmonth, thisyear, custom
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public OperatorType? Operator { get; set; }
}
