using System.Text.RegularExpressions;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Factories;

public class WeStrategy : IOperatorStrategy
{
    public OperatorType Operator => OperatorType.WE;
    public string DefaultBalanceInquiryCommand => "*550#";
    public string DefaultWalletBalanceInquiryCommand => "*7*1*2*{Password}#";
    public string DefaultNumberInquiryCommand => "*688#";

    public decimal? ExtractBalance(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(?:رصيدك|balance|الرصيد|كاش|الحالي|الحالى)\s*:?\s*([\d\.]+)", RegexOptions.IgnoreCase);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var bal))
            return bal;

        var numMatch = Regex.Match(response, @"([\d]+(?:\.[\d]+)?)");
        return numMatch.Success && decimal.TryParse(numMatch.Groups[1].Value, out var fbal) ? fbal : null;
    }

    public string? ExtractPhoneNumber(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(015\d{8})");
        return match.Success ? match.Groups[1].Value : null;
    }
}
