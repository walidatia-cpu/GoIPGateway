using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GoIPGateway.Infrastructure.Services;

public class SignalRNotifier : ISignalRNotifier
{
    private readonly IHubContext<GatewayHub> _hubContext;
    private readonly ILogger<SignalRNotifier> _logger;

    public SignalRNotifier(IHubContext<GatewayHub> hubContext, ILogger<SignalRNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyTransactionUpdatedAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                transaction.Id,
                transaction.TransactionRef,
                transaction.ExternalRef,
                transaction.TenantId,
                transaction.Status,
                StatusName = transaction.Status.ToString(),
                transaction.Amount,
                transaction.SimBalanceBefore,
                transaction.SimBalanceAfter,
                transaction.RawResponse,
                transaction.ErrorMessage,
                transaction.CompletedAt
            };

            // Notify specific tenant and admins
            await _hubContext.Clients.Group($"Tenant_{transaction.TenantId}").SendAsync("OnTransactionUpdated", payload, cancellationToken);
            await _hubContext.Clients.Group("SuperAdmins").SendAsync("OnTransactionUpdated", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR transaction update for {TxRef}", transaction.TransactionRef);
        }
    }

    public async Task NotifySimBalanceChangedAsync(SimCard simCard, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                simCard.Id,
                simCard.PhoneNumber,
                simCard.Operator,
                OperatorName = simCard.Operator.ToString(),
                simCard.CurrentBalance,
                simCard.PreviousBalance,
                simCard.LastBalanceCheck
            };

            await _hubContext.Clients.All.SendAsync("OnSimBalanceChanged", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR SIM balance change for SIM {SimId}", simCard.Id);
        }
    }

    public async Task NotifyPortStatusChangedAsync(DevicePort port, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                port.Id,
                port.DeviceId,
                port.PortNumber,
                port.PortLabel,
                port.Status,
                StatusName = port.Status.ToString(),
                port.IsBusy,
                port.BusySince
            };

            await _hubContext.Clients.All.SendAsync("OnPortStatusChanged", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR port status change for Port {PortId}", port.Id);
        }
    }

    public async Task NotifyDeviceStatusChangedAsync(Device device, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                device.Id,
                device.DeviceName,
                device.IsOnline,
                device.LastHeartbeat,
                device.ActivePorts,
                device.TotalPorts
            };

            await _hubContext.Clients.All.SendAsync("OnDeviceStatusChanged", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR device status change for Device {DeviceId}", device.Id);
        }
    }

    public async Task SendLiveFeedMessageAsync(string eventType, object payload, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = new
            {
                EventType = eventType,
                Data = payload,
                Timestamp = DateTime.UtcNow
            };

            if (tenantId.HasValue)
            {
                await _hubContext.Clients.Group($"Tenant_{tenantId.Value}").SendAsync("OnLiveFeed", msg, cancellationToken);
            }

            await _hubContext.Clients.Group("SuperAdmins").SendAsync("OnLiveFeed", msg, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send live feed message");
        }
    }
}

public class GatewayHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
    }

    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "SuperAdmins");
    }
}

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(HttpClient httpClient, ILogger<WebhookDispatcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> DispatchTransactionWebhookAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transaction.CallbackUrl))
            return true; // No callback configured

        try
        {
            var payload = new
            {
                @event = "transaction.completed",
                transactionRef = transaction.TransactionRef,
                externalRef = transaction.ExternalRef,
                status = transaction.Status.ToString(),
                result = new
                {
                    success = transaction.Status == Domain.Enums.TransactionStatus.Completed,
                    rawResponse = transaction.RawResponse,
                    simBalanceBefore = transaction.SimBalanceBefore,
                    simBalanceAfter = transaction.SimBalanceAfter,
                    bankBalance = transaction.BankBalanceAfter
                },
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, transaction.CallbackUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // HMAC signature header
            var secret = "default-secret"; // or tenant secret
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(json))).ToLower();
            request.Headers.Add("X-Signature", hash);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            _logger.LogInformation("Dispatched Webhook for Tx {TxRef} to {Url} with status code {Code}",
                transaction.TransactionRef, transaction.CallbackUrl, response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook dispatch failed for Tx {TxRef} to {Url}", transaction.TransactionRef, transaction.CallbackUrl);
            return false;
        }
    }
}

public interface IJwtTokenService
{
    string GenerateToken(string userId, string userName, string role, int? tenantId, string? allowedServices = null);
}

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
