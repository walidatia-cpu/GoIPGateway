using GoIPGateway.Domain.Entities;

namespace GoIPGateway.Application.Common.Interfaces;

public interface IGoipCommunicationClient
{
    Task<string> SendUssdCommandAsync(Device device, int portNumber, string ussdCommand, int timeoutSeconds = 30, CancellationToken cancellationToken = default);
    Task<bool> CheckDeviceHealthAsync(Device device, CancellationToken cancellationToken = default);
    Task<List<SmsHistory>> PollSmsMessagesAsync(Device device, CancellationToken cancellationToken = default);
}
