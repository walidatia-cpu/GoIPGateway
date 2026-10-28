using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class ServiceParameter : BaseEntity
{
    public int ServiceDefinitionId { get; set; }
    public virtual ServiceDefinition ServiceDefinition { get; set; } = null!;

    public string ParameterName { get; set; } = string.Empty; // e.g. "Amount"
    public string ParameterKey { get; set; } = string.Empty; // e.g. "amount"
    public string? DisplayNameAr { get; set; }
    public string? DisplayNameEn { get; set; }

    public ParameterDataType DataType { get; set; } = ParameterDataType.String;
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? ValidationRegex { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
