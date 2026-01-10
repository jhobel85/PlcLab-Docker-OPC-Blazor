using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.OPC;

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

public class LiveSignalSubscriptionService(IOpcUaClientFactory factory) : ILiveSignalSubscriptionService
{
    private readonly IOpcUaClientFactory _factory = factory;

    public async Task<Subscription> SubscribeAsync(
        Session session,
        IEnumerable<(string label, string path)> signals,
        Action<string, object?, DateTime> onValue,
        CancellationToken ct = default)
    {
        var subscription = await _factory.CreateSubscriptionAsync(session, ct);

        foreach (var (label, path) in signals)
        {
            var nodeId = await _factory.ResolveNodeIdAsync(session, path, ct);

            // Push initial value immediately so UI shows something even before changes
            var initial = await _factory.ReadValueAsync(session, nodeId, ct);
            onValue(label, initial, DateTime.UtcNow);

            await _factory.AddMonitoredItemAsync(subscription, nodeId, (item, args) =>
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
