using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Services.Transactions;

public class TransactionQueryService : ITransactionQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TransactionResultDto>> GetByRefAsync(string transactionRef, int? tenantId = null, bool isSuperAdmin = false, CancellationToken cancellationToken = default)
    {
        var tx = await _unitOfWork.Transactions.GetByRefWithLogsAsync(transactionRef, cancellationToken);
        if (tx == null)
            return Result.Failure<TransactionResultDto>("Transaction.NotFound", "العملية غير موجودة.");

        if (tenantId.HasValue && !isSuperAdmin && tx.TenantId != tenantId.Value)
            return Result.Failure<TransactionResultDto>("Transaction.AccessDenied", "ليس لديك صلاحية الوصول لهذه العملية.");

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

    public async Task<Result<PagedResult<TransactionResultDto>>> GetPagedListAsync(
        int? tenantId,
        int? simCardId,
        TransactionStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _unitOfWork.Transactions.GetPagedTransactionsAsync(
            tenantId, simCardId, status, fromDate, toDate, pageNumber, pageSize, cancellationToken);

        var dtos = items.Select(tx => new TransactionResultDto
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
        }).ToList();

        var paged = new PagedResult<TransactionResultDto>(dtos, totalCount, pageNumber, pageSize);
        return Result.Success(paged);
    }
}
