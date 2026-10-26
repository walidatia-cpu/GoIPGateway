using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Features.Transactions.Queries;

public record GetTransactionByRefQuery(string TransactionRef) : IRequest<Result<TransactionResultDto>>;

public class GetTransactionByRefQueryHandler : IRequestHandler<GetTransactionByRefQuery, Result<TransactionResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetTransactionByRefQueryHandler(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<TransactionResultDto>> Handle(GetTransactionByRefQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;

        var query = _context.TenantTransactions
            .Include(t => t.Logs.OrderBy(l => l.StepOrder))
            .AsNoTracking()
            .Where(t => t.TransactionRef == request.TransactionRef);

        if (tenantId.HasValue && !_currentUser.IsSuperAdmin)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }

        var tx = await query.FirstOrDefaultAsync(cancellationToken);
        if (tx == null)
            return Result.Failure<TransactionResultDto>("Transaction.NotFound", "العملية غير موجودة.");

        var dto = new TransactionResultDto
        {
            TransactionId = tx.Id,
            TransactionRef = tx.TransactionRef,
            ExternalRef = tx.ExternalRef,
            Status = tx.Status,
            Message = tx.ErrorMessage ?? (tx.Status == TransactionStatus.Completed ? "تمت العملية بنجاح" : null),
            ErrorCode = tx.ErrorCode,
            RawResponse = tx.RawResponse,
            SimBalanceBefore = tx.SimBalanceBefore,
            SimBalanceAfter = tx.SimBalanceAfter,
            BankBalanceBefore = tx.BankBalanceBefore,
            BankBalanceAfter = tx.BankBalanceAfter,
            CompletedAt = tx.CompletedAt ?? tx.RequestedAt,
            Logs = tx.Logs.Select(l => new TransactionLogDto
            {
                StepOrder = l.StepOrder,
                StepName = l.StepName,
                Command = l.UssdCommand,
                Response = l.UssdResponse,
                IsSuccess = l.IsSuccess,
                DurationMs = l.DurationMs
            }).ToList()
        };

        return Result.Success(dto);
    }
}

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
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GetTransactionsListQueryHandler(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<PagedResult<TransactionResultDto>>> Handle(GetTransactionsListQuery request, CancellationToken cancellationToken)
    {
        var activeTenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;

        var query = _context.TenantTransactions.AsNoTracking().AsQueryable();

        if (activeTenantId.HasValue && !_currentUser.IsSuperAdmin)
        {
            query = query.Where(t => t.TenantId == activeTenantId.Value);
        }
        else if (request.TenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == request.TenantId.Value);
        }

        if (request.SimCardId.HasValue)
            query = query.Where(t => t.SimCardId == request.SimCardId.Value);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            query = query.Where(t => t.RequestedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(t => t.RequestedAt <= request.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.RequestedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(tx => new TransactionResultDto
            {
                TransactionId = tx.Id,
                TransactionRef = tx.TransactionRef,
                ExternalRef = tx.ExternalRef,
                Status = tx.Status,
                Message = tx.ErrorMessage,
                ErrorCode = tx.ErrorCode,
                RawResponse = tx.RawResponse,
                SimBalanceBefore = tx.SimBalanceBefore,
                SimBalanceAfter = tx.SimBalanceAfter,
                CompletedAt = tx.CompletedAt ?? tx.RequestedAt
            })
            .ToListAsync(cancellationToken);

        var paged = new PagedResult<TransactionResultDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Result.Success(paged);
    }
}
