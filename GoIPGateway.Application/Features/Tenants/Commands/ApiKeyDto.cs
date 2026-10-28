namespace GoIPGateway.Application.Features.Tenants.Commands;

public class ApiKeyDto
{
    public int Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public int RateLimit { get; set; }
}
