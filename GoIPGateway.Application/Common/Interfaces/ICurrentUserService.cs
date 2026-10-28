namespace GoIPGateway.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    int? TenantId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
    string? AllowedServices { get; }
}
