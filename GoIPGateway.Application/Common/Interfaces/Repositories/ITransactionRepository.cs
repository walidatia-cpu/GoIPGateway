using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Common.Interfaces.Repositories;

public interface ITransactionRepository : IRepository<TenantTransaction, long>
{
    Task<TenantTransaction?> GetByRefWithLogsAsync(string transactionRef, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<TenantTransaction> Items, int TotalCount)> GetPagedTransactionsAsync(
        int? tenantId,
        int? simCardId,
        TransactionStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
