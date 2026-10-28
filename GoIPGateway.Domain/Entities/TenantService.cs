using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

public class TenantService : BaseEntity
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public int ServiceDefinitionId { get; set; }
    public virtual ServiceDefinition ServiceDefinition { get; set; } = null!;

    public decimal? CustomPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DailyLimit { get; set; }
    public int? MonthlyLimit { get; set; }

    public string? CustomConfig { get; set; } // JSON additional settings

    [NotMapped]
    public List<int> AssignedSimCardIds
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CustomConfig)) return new List<int>();
            try
            {
                using var doc = JsonDocument.Parse(CustomConfig);
                if (doc.RootElement.TryGetProperty("AssignedSimCardIds", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    return arr.EnumerateArray().Select(x => x.GetInt32()).ToList();
                }
            }
            catch { }
            return new List<int>();
        }
        set
        {
            var dict = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(CustomConfig))
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(CustomConfig);
                    if (existing != null)
                    {
                        foreach (var kvp in existing) dict[kvp.Key] = kvp.Value;
                    }
                }
                catch { }
            }
            dict["AssignedSimCardIds"] = value;
            CustomConfig = JsonSerializer.Serialize(dict);
        }
    }

    public string? WebhookUrl { get; set; }
    public string? WebhookSecret { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public string? AssignedById { get; set; }
}
