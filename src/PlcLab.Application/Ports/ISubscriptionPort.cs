using System;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;

namespace PlcLab.Application.Ports
{
    public interface ISubscriptionPort
    {
        Task<Subscription> CreateSubscriptionAsync(Session session, CancellationToken ct = default);
        Task AddMonitoredItemAsync(Subscription subscription, NodeId nodeId, Action<MonitoredItem, MonitoredItemNotificationEventArgs> callback, CancellationToken ct = default);
    }
}
