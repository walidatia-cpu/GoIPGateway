namespace GoIPGateway.Application.DTOs;

public class TransactionLogDto
{
    public int StepOrder { get; set; }
    public string? StepName { get; set; }
    public string? Command { get; set; }
    public string? Response { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
}
