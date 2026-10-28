using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GoIPGateway.Domain.Common;

namespace GoIPGateway.Domain.Entities;

public class ServiceUssdStep : BaseEntity, ISoftDelete
{
    public int ServiceDefinitionId { get; set; }
    public virtual ServiceDefinition ServiceDefinition { get; set; } = null!;

    public int StepOrder { get; set; } = 1;
    public string? StepTitle { get; set; }
    public string UssdCommand { get; set; } = string.Empty; // e.g. "*9*{MerchantCode}*{Amount}*{Password}#"

    public string? SuccessPattern { get; set; }
    public string? FailurePattern { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 1;
    public int DelayAfterMs { get; set; } = 1000;

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public List<UssdRuleItem> ParsedSuccessRules => ParseRuleList(SuccessPattern);

    [NotMapped]
    public List<UssdRuleItem> ParsedFailureRules => ParseRuleList(FailurePattern);

    [NotMapped]
    public UssdSuccessRule ParsedSuccessRule
    {
        get
        {
            var rules = ParsedSuccessRules;
            return new UssdSuccessRule
            {
                Patterns = rules.Select(r => r.Pattern).Where(p => !string.IsNullOrWhiteSpace(p)).ToList(),
                ClientMessage = rules.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.ClientMessage))?.ClientMessage
            };
        }
    }

    public static List<UssdRuleItem> ParseRuleList(string? pattern)
    {
        var list = new List<UssdRuleItem>();
        if (string.IsNullOrWhiteSpace(pattern)) return list;

        var trimmed = pattern.Trim();
        if (trimmed.StartsWith("["))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<UssdRuleItem>>(trimmed, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null && parsed.Any()) return parsed;
            }
            catch { }
        }
        else if (trimmed.StartsWith("{"))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;
                string? clientMsg = root.TryGetProperty("ClientMessage", out var cm) ? cm.GetString() : null;
                if (root.TryGetProperty("Patterns", out var pats) && pats.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in pats.EnumerateArray())
                    {
                        var str = p.GetString();
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            list.Add(new UssdRuleItem { Pattern = str, ClientMessage = clientMsg });
                        }
                    }
                    if (list.Any()) return list;
                }
            }
            catch { }
        }

        var lines = pattern.Split(new[] { '\r', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var l in lines)
        {
            list.Add(new UssdRuleItem { Pattern = l });
        }
        return list;
    }
}
