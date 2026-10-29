using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Features.Devices.Commands;

namespace GoIPGateway.Application.Services.Devices;

public interface IDeviceCommandService
{
    Task<Result<int>> RegisterDeviceAsync(RegisterDeviceCommand command, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateDeviceAsync(int id, string deviceName, string ipAddress, int port, string username, string password, string? location, string? notes, bool isActive, string? merchantCode = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteDeviceAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> RestoreDeviceAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> AssignDeviceToTenantAsync(int deviceId, int tenantId, int? portStart, int? portEnd, CancellationToken cancellationToken = default);
    Task<Result<bool>> UnassignDeviceFromTenantAsync(int deviceId, int tenantId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SwitchActiveGroupAsync(int deviceId, string group, CancellationToken cancellationToken = default);
    Task<Result<bool>> SwitchBlockSlotAsync(int deviceId, int portId, CancellationToken cancellationToken = default);
}
