using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Entities;

public class Device : BaseEntity, ISoftDelete, IAuditable
{
    public string DeviceName { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; } = DeviceType.GoIP128;
    public string? SerialNumber { get; set; }

    // Network / Auth details
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public int HttpPort { get; set; } = 80;
    public int TelnetPort { get; set; } = 23;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin"; // encrypted

    // Health
    public bool IsOnline { get; set; } = false;
    public DateTime? LastHeartbeat { get; set; }
    public int TotalPorts { get; set; } = 128;
    public int ActivePorts { get; set; } = 0;
    public string? FirmwareVersion { get; set; }

    public string? Notes { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<DevicePort> Ports { get; set; } = new List<DevicePort>();
    public virtual ICollection<TenantDevice> AssignedTenants { get; set; } = new List<TenantDevice>();
}
