using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Web.Models;

public class ExecuteServiceViewModel
{
    public string ServiceCode { get; set; } = string.Empty;
    public int? SimCardId { get; set; }
    public OperatorType? Operator { get; set; }
    public string? ExternalRef { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();

    // Catalog for UI
    public List<ServiceDefinition> Services { get; set; } = new();
    public List<SimCard> AvailableSims { get; set; } = new();
}
