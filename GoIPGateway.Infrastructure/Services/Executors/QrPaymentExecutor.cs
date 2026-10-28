using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Factories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Infrastructure.Services.Executors;

public class QrPaymentExecutor : IServiceExecutor
{
    private readonly UssdMultiStepExecutor _ussdExecutor;

    public QrPaymentExecutor(UssdMultiStepExecutor ussdExecutor)
    {
        _ussdExecutor = ussdExecutor;
    }

    public bool CanHandle(ServiceType serviceType) => serviceType == ServiceType.QRPayment;

    public Task<Result<TransactionResultDto>> ExecuteAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        return _ussdExecutor.ExecuteAsync(transaction, cancellationToken);
    }
}
