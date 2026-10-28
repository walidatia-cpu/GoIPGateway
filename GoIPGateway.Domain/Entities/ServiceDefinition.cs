using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class ServiceDefinition : BaseEntity, ISoftDelete, IAuditable
{
    public string ServiceCode { get; set; } = string.Empty; // e.g. "BAL_INQ", "QR_PAY"
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }

    public ServiceType ServiceType { get; set; } = ServiceType.USSD;
    public ServiceCategory Category { get; set; } = ServiceCategory.Payment;

    public OperatorType? Operator { get; set; } // null = all operators
    public BalanceActionType BalanceActionType { get; set; } = BalanceActionType.None;

    public decimal BaseCost { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool RequiresApproval { get; set; } = false;
    public bool IsSystemService { get; set; } = false;

    public int SortOrder { get; set; } = 0;
    public string? IconClass { get; set; }

    // Navigation
    public virtual ICollection<ServiceUssdStep> UssdSteps { get; set; } = new List<ServiceUssdStep>();
    public virtual ICollection<ServiceParameter> Parameters { get; set; } = new List<ServiceParameter>();
    public virtual ICollection<TenantService> TenantSubscriptions { get; set; } = new List<TenantService>();
    public virtual ICollection<TenantTransaction> Transactions { get; set; } = new List<TenantTransaction>();
}
