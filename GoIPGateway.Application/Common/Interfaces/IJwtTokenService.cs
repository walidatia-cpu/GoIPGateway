namespace GoIPGateway.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string userName, string role, int? tenantId, string? allowedServices = null);
}
