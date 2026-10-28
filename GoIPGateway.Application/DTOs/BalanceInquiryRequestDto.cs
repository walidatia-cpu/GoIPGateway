using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs;

public class BalanceInquiryRequestDto
{
    public int? SimCardId { get; set; }
    public OperatorType? Operator { get; set; }
}
