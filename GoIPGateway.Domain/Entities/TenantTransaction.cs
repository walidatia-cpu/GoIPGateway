using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class TenantTransaction : BaseEntity<long>
{
    public string TransactionRef { get; set; } = string.Empty; // Unique UUID e.g. "GW-20261026-XXXX"
    public string? ExternalRef { get; set; }

    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public int ServiceDefinitionId { get; set; }
    public virtual ServiceDefinition ServiceDefinition { get; set; } = null!;

    public int? SimCardId { get; set; }
    public virtual SimCard? SimCard { get; set; }

    public int? DevicePortId { get; set; }
    public virtual DevicePort? DevicePort { get; set; }

    // Payload & Result
    public string? Parameters { get; set; } // JSON
    public decimal? Amount { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public string? RawResponse { get; set; }
    public string? ParsedResult { get; set; } // JSON
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    // Balances tracking
    public decimal? SimBalanceBefore { get; set; }
    public decimal? SimBalanceAfter { get; set; }
    public decimal? BankBalanceBefore { get; set; }
    public decimal? BankBalanceAfter { get; set; }

    // Financial charges
    public decimal ServiceCost { get; set; } = 0;
    public decimal TenantCharge { get; set; } = 0;

    // Execution timestamps
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Caller audit
    public string? RequestedById { get; set; }
    public string? RequestIP { get; set; }
    public RequestSource RequestSource { get; set; } = RequestSource.API;

    // Webhook tracking
    public string? CallbackUrl { get; set; }
    public DateTime? WebhookSentAt { get; set; }
    public int WebhookStatus { get; set; } = 0; // 0=Pending, 1=Sent, 2=Failed
    public int WebhookRetryCount { get; set; } = 0;

    // Navigation
    public virtual ICollection<TransactionLog> Logs { get; set; } = new List<TransactionLog>();
}

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
