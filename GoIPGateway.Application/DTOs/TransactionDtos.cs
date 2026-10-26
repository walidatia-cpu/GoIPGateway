using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs;

public class TransactionResultDto
{
    public long TransactionId { get; set; }
    public string TransactionRef { get; set; } = string.Empty;
    public string? ExternalRef { get; set; }
    public TransactionStatus Status { get; set; }
    public bool IsSuccess => Status == TransactionStatus.Completed;
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public string? RawResponse { get; set; }
    public Dictionary<string, object>? ParsedData { get; set; }

    public decimal? SimBalanceBefore { get; set; }
    public decimal? SimBalanceAfter { get; set; }
    public decimal? BankBalanceBefore { get; set; }
    public decimal? BankBalanceAfter { get; set; }

    public DateTime CompletedAt { get; set; }
    public List<TransactionLogDto> Logs { get; set; } = new();
}

public class TransactionLogDto
{
    public int StepOrder { get; set; }
    public string? StepName { get; set; }
    public string? Command { get; set; }
    public string? Response { get; set; }
    public bool IsSuccess { get; set; }
    public int DurationMs { get; set; }
}

public class ExecuteServiceRequestDto
{
    public string ServiceCode { get; set; } = string.Empty;
    public string? ExternalRef { get; set; }
    public int? SimCardId { get; set; }
    public OperatorType? Operator { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string? CallbackUrl { get; set; }
}

public class BalanceInquiryRequestDto
{
    public int? SimCardId { get; set; }
    public OperatorType? Operator { get; set; }
}

public class SimNumberInquiryRequestDto
{
    public int SimCardId { get; set; }
}
