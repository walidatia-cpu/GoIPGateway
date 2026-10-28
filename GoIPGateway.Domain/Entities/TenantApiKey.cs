using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

public class TenantApiKey : BaseEntity
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? KeyName { get; set; }

    public string? AllowedIPs { get; set; } // JSON array of IPs or CIDRs
    public string? AllowedServices { get; set; } // JSON array of allowed ServiceDefinition IDs (null = all)
    public int RateLimit { get; set; } = 100; // Requests per minute

    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
