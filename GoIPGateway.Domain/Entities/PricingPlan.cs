using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

public class PricingPlan : BaseEntity
{
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal MonthlyFee { get; set; } = 0;
    public int MaxDevices { get; set; } = 1;
    public int MaxDailyOps { get; set; } = 1000;
    public int MaxMonthlyOps { get; set; } = 30000;
    public decimal CostPerOperation { get; set; } = 0.10m;

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}
