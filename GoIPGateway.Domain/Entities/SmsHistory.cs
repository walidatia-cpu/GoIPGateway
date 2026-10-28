using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class SmsHistory : BaseEntity<long>
{
    public int? SimCardId { get; set; }
    public virtual SimCard? SimCard { get; set; }

    public int? DevicePortId { get; set; }
    public virtual DevicePort? DevicePort { get; set; }

    public SmsDirection Direction { get; set; } = SmsDirection.Incoming;
    public string? SenderNumber { get; set; }
    public string? ReceiverNumber { get; set; }
    public string MessageText { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public bool IsProcessed { get; set; } = false;
}
