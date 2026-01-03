
using Opc.Ua;
using Opc.Ua.Client;

namespace PlcLab.OPC
{
    public interface IOpcUaClientFactory
    {
        Task<Session> CreateSessionAsync(string discoveryUrl, bool useSecurity = true, CancellationToken ct = default);
        string GetApplicationName();

        // Browsing
        Task<NodeId> ResolveNodeIdAsync(Session session, string path, CancellationToken ct = default);
        Task<ReferenceDescriptionCollection> BrowseAsync(Session session, NodeId nodeId, CancellationToken ct = default);

        // Subscriptions
        Task<Subscription> CreateSubscriptionAsync(Session session, CancellationToken ct = default);
        Task AddMonitoredItemAsync(Subscription subscription, NodeId nodeId, Action<MonitoredItem, MonitoredItemNotificationEventArgs> callback, CancellationToken ct = default);

        // Read/Write
        Task<object> ReadValueAsync(Session session, NodeId nodeId, CancellationToken ct = default);
        Task WriteValueAsync(Session session, NodeId nodeId, Variant value, CancellationToken ct = default);

        // Methods
        Task<Variant[]> CallMethodAsync(Session session, NodeId objectId, NodeId methodId, Variant[] inputArgs, CancellationToken ct = default);
    }
}