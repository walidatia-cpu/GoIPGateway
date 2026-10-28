using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Common.Interfaces;

public interface IWebhookDispatcher
{
    Task<bool> DispatchTransactionWebhookAsync(TenantTransaction transaction, CancellationToken cancellationToken = default);
}
