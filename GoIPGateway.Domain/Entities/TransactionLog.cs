using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

public class TransactionLog : BaseEntity<long>
{
    public long TransactionId { get; set; }
    public virtual TenantTransaction Transaction { get; set; } = null!;

    public int StepOrder { get; set; }
    public string? StepName { get; set; }
    public string? UssdCommand { get; set; }
    public string? UssdResponse { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
