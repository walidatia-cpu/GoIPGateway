using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Services.Transactions;
using GoIPGateway.Domain.Enums;
using MediatR;

namespace GoIPGateway.Application.Features.Transactions.Queries;

public record GetTransactionsListQuery(
    int? TenantId = null,
    int? SimCardId = null,
    TransactionStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<TransactionResultDto>>>;

public class GetTransactionsListQueryHandler : IRequestHandler<GetTransactionsListQuery, Result<PagedResult<TransactionResultDto>>>
{
    private readonly ITransactionQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetTransactionsListQueryHandler(ITransactionQueryService queryService, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<PagedResult<TransactionResultDto>>> Handle(GetTransactionsListQuery request, CancellationToken cancellationToken)
    {
        var activeTenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        var effectiveTenantId = (activeTenantId.HasValue && !_currentUser.IsSuperAdmin) ? activeTenantId : request.TenantId;

        return await _queryService.GetPagedListAsync(
            effectiveTenantId,
            request.SimCardId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
