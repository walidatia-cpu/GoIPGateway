using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using GoIPGateway.Application.Common.Interfaces;

namespace GoIPGateway.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string userId, string userName, string role, int? tenantId, string? allowedServices = null)
    {
        var secret = _configuration["Jwt:SecretKey"] ?? "GoIP_Gateway_Super_Secret_Key_For_Jwt_Generation_2026_Secure_Default!";
        var issuer = _configuration["Jwt:Issuer"] ?? "GoIPGatewayPlatform";
        var audience = _configuration["Jwt:Audience"] ?? "GoIPGatewayClients";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var exp) ? exp : 1440;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Role, role)
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim("TenantId", tenantId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(allowedServices))
        {
            claims.Add(new Claim("AllowedServices", allowedServices));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
