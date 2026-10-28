using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Services.Transactions;
using MediatR;

namespace GoIPGateway.Application.Features.Transactions.Queries;

public record GetTransactionByRefQuery(string TransactionRef) : IRequest<Result<TransactionResultDto>>;

public class GetTransactionByRefQueryHandler : IRequestHandler<GetTransactionByRefQuery, Result<TransactionResultDto>>
{
    private readonly ITransactionQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetTransactionByRefQueryHandler(ITransactionQueryService queryService, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<TransactionResultDto>> Handle(GetTransactionByRefQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        return await _queryService.GetByRefAsync(request.TransactionRef, tenantId, _currentUser.IsSuperAdmin, cancellationToken);
    }
}
