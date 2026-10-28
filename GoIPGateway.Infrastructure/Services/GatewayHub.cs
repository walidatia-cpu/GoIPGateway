using Microsoft.AspNetCore.SignalR;

namespace GoIPGateway.Infrastructure.Services;

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
