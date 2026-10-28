using System.Text.RegularExpressions;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Factories;

public class EtisalatStrategy : IOperatorStrategy
{
    public OperatorType Operator => OperatorType.Etisalat;
    public string DefaultBalanceInquiryCommand => "*555#";
    public string DefaultWalletBalanceInquiryCommand => "*777*4*{Password}#";
    public string DefaultNumberInquiryCommand => "*947#";

    public decimal? ExtractBalance(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(?:رصيدك هو|رصيدك|balance is|الرصيد|كاش|الحالي|الحالى)\s*:?\s*([\d\.]+)", RegexOptions.IgnoreCase);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var bal))
            return bal;

        var numMatch = Regex.Match(response, @"([\d]+(?:\.[\d]+)?)");
        return numMatch.Success && decimal.TryParse(numMatch.Groups[1].Value, out var fbal) ? fbal : null;
    }

    public string? ExtractPhoneNumber(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(011\d{8})");
        return match.Success ? match.Groups[1].Value : null;
    }
}
