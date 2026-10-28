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
