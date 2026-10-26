using System.Text.RegularExpressions;
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

public class OrangeStrategy : IOperatorStrategy
{
    public OperatorType Operator => OperatorType.Orange;
    public string DefaultBalanceInquiryCommand => "#100#3#";
    public string DefaultWalletBalanceInquiryCommand => "#7115*81*{Password}#";
    public string DefaultNumberInquiryCommand => "#119*1#";

    public decimal? ExtractBalance(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(?:رصيدك|Your balance is|مبلغ|الرصيد|كاش|الحالي|الحالى)\s*:?\s*([\d\.]+)", RegexOptions.IgnoreCase);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var bal))
            return bal;

        var numMatch = Regex.Match(response, @"([\d]+(?:\.[\d]+)?)");
        return numMatch.Success && decimal.TryParse(numMatch.Groups[1].Value, out var fbal) ? fbal : null;
    }

    public string? ExtractPhoneNumber(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;
        var match = Regex.Match(response, @"(012\d{8})");
        return match.Success ? match.Groups[1].Value : null;
    }
}

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

public interface IOperatorStrategyFactory
{
    IOperatorStrategy GetStrategy(OperatorType op);
}

public class OperatorStrategyFactory : IOperatorStrategyFactory
{
    private readonly IEnumerable<IOperatorStrategy> _strategies;

    public OperatorStrategyFactory(IEnumerable<IOperatorStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IOperatorStrategy GetStrategy(OperatorType op)
    {
        var strategy = _strategies.FirstOrDefault(s => s.Operator == op);
        return strategy ?? _strategies.First(s => s.Operator == OperatorType.Vodafone);
    }
}
