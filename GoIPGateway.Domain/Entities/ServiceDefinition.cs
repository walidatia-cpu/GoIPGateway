using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class UssdRuleItem
{
    public string Pattern { get; set; } = string.Empty;
    public string? ClientMessage { get; set; }
}

public class UssdFailureRule : UssdRuleItem { }

public class UssdSuccessRule
{
    public List<string> Patterns { get; set; } = new();
    public string? ClientMessage { get; set; }
}

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
