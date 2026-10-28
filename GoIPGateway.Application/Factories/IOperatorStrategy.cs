using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Factories;

public interface IOperatorStrategy
{
    OperatorType Operator { get; }
    string DefaultBalanceInquiryCommand { get; }
    string DefaultWalletBalanceInquiryCommand { get; }
    string DefaultNumberInquiryCommand { get; }

    decimal? ExtractBalance(string response);
    string? ExtractPhoneNumber(string response);
}
