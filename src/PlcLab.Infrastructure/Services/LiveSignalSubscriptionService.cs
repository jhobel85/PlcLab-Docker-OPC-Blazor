using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.Application.Ports;

namespace PlcLab.Infrastructure.Services;

public interface ILiveSignalSubscriptionService
{
    Task<Subscription> SubscribeAsync(
        Session session,
        IEnumerable<(string label, string path)> signals,
        Action<string, object?, DateTime> onValue,
        CancellationToken ct = default);

    Task UnsubscribeAsync(Subscription? subscription, CancellationToken ct = default);
}

public class LiveSignalSubscriptionService(IBrowsePort browsePort, IReadWritePort readWritePort, ISubscriptionPort subscriptionPort) : ILiveSignalSubscriptionService
{
    private readonly IBrowsePort _browsePort = browsePort;
    private readonly IReadWritePort _readWritePort = readWritePort;
    private readonly ISubscriptionPort _subscriptionPort = subscriptionPort;

    public async Task<Subscription> SubscribeAsync(
        Session session,
        IEnumerable<(string label, string path)> signals,
        Action<string, object?, DateTime> onValue,
        CancellationToken ct = default)
    {
        var subscription = await _subscriptionPort.CreateSubscriptionAsync(session, ct);

        foreach (var (label, path) in signals)
        {
            var nodeId = await _browsePort.ResolveNodeIdAsync(session, path, ct);

            // Push initial value immediately so UI shows something even before changes
            var initial = await _readWritePort.ReadValueAsync(session, nodeId, ct);
            onValue(label, initial, DateTime.UtcNow);

            await _subscriptionPort.AddMonitoredItemAsync(subscription, nodeId, (item, args) =>
            {
                if (args.NotificationValue is MonitoredItemNotification data)
                {
                    var dv = data.Value;
                    var timestamp = dv?.SourceTimestamp ?? DateTime.UtcNow;
                    onValue(label, dv?.Value, timestamp);
                }
            }, ct);
        }

        return subscription;
    }

    public async Task UnsubscribeAsync(Subscription? subscription, CancellationToken ct = default)
    {
        if (subscription == null)
        {
            return;
        }

        await subscription.DeleteAsync(true, ct);
    }
}
