using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class DevicePort : BaseEntity
{
    public int DeviceId { get; set; }
    public virtual Device Device { get; set; } = null!;

    public int PortNumber { get; set; } // 1-128 or 1-512
    public string PortLabel { get; set; } = string.Empty; // e.g. "L001"

    public PortStatus Status { get; set; } = PortStatus.Empty;
    public bool IsBusy { get; set; } = false;
    public DateTime? BusySince { get; set; }
    public long? CurrentOperationId { get; set; }

    public bool IsActive { get; set; } = true;

    // GoIP Block & Multi-SIM Bank Slot mapping:
    // GoIP 128: 32 blocks × 4 slots (1.01-1.04 / 1A-1D)
    // GoIP 512: 64 blocks × 8 slots (1.01-1.08 / 1A-1H)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int SlotsPerBlock => (Device?.DeviceType == DeviceType.GoIP512 || (Device == null && PortNumber > 128)) ? 8 : 4;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int BlockNumber => ((PortNumber - 1) / SlotsPerBlock) + 1;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int SlotIndex => (PortNumber - 1) % SlotsPerBlock;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int SlotNumber => SlotIndex + 1;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public char SlotLetter => (char)('A' + SlotIndex);

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DotNotation => $"{BlockNumber}.{SlotNumber:D2}";

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string LetterNotation => $"{BlockNumber}{SlotLetter}";

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DisplayPortLabel => $"{DotNotation} ({LetterNotation})";

    // Navigation
    public virtual SimCard? SimCard { get; set; }
    public virtual ICollection<TenantTransaction> Transactions { get; set; } = new List<TenantTransaction>();
}
