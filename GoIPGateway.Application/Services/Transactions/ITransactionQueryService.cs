using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Services.Transactions;

public interface ITransactionQueryService
{
    Task<Result<TransactionResultDto>> GetByRefAsync(string transactionRef, int? tenantId = null, bool isSuperAdmin = false, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<TransactionResultDto>>> GetPagedListAsync(
        int? tenantId,
        int? simCardId,
        TransactionStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
