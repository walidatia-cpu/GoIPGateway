using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs.Reports;

public class CompanyReportFilterDto
{
    public int? TenantId { get; set; }
    public int? DeviceId { get; set; }
    public int? SimCardId { get; set; }
    public OperatorType? Operator { get; set; }
    public int? ServiceDefinitionId { get; set; }
    public TransactionStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? DatePreset { get; set; } = "thismonth"; // today, yesterday, last7days, thismonth, lastmonth, custom

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
