using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.Transactions.Commands;

namespace GoIPGateway.Application.Services.Transactions;

public interface ITransactionCommandService
{
    Task<Result<TransactionResultDto>> ExecuteServiceAsync(ExecuteServiceCommand command, CancellationToken cancellationToken = default);
}
