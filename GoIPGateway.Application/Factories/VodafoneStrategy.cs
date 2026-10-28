using System.Text.RegularExpressions;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Factories;

public class VodafoneStrategy : IOperatorStrategy
{
    public OperatorType Operator => OperatorType.Vodafone;
    public string DefaultBalanceInquiryCommand => "*868*1#";
    public string DefaultWalletBalanceInquiryCommand => "*9*7*{Password}#";
    public string DefaultNumberInquiryCommand => "*878#";

    public decimal? ExtractBalance(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(?:رصيدك هو|رصيدك الحالي|Your balance is|الرصيد المتاح هو|رصيدك)\s*:?\s*([\d\.]+)", RegexOptions.IgnoreCase);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var bal))
            return bal;

        // Fallback: extract first decimal/number
        var numMatch = Regex.Match(response, @"([\d]+(?:\.[\d]+)?)");
        if (numMatch.Success && decimal.TryParse(numMatch.Groups[1].Value, out var fbal))
            return fbal;

        return null;
    }

    public string? ExtractPhoneNumber(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(010\d{8})");
        return match.Success ? match.Groups[1].Value : null;
    }
}
