using System;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.Application.Ports;

namespace PlcLab.OPC.Adapters
{
    public class OpcSubscriptionAdapter : ISubscriptionPort
    {
        public async Task<Subscription> CreateSubscriptionAsync(Session session, CancellationToken ct = default)
        {
            var subscription = new Subscription(session.DefaultSubscription)
            {
                PublishingInterval = 1000,
                KeepAliveCount = 10,
                LifetimeCount = 100,
                MaxNotificationsPerPublish = 1000,
                PublishingEnabled = true,
                TimestampsToReturn = TimestampsToReturn.Both
            };
            session.AddSubscription(subscription);
            await subscription.CreateAsync(ct);
            return subscription;
        }

        public async Task AddMonitoredItemAsync(Subscription subscription, NodeId nodeId, Action<MonitoredItem, MonitoredItemNotificationEventArgs> callback, CancellationToken ct = default)
        {
            var monitoredItem = new MonitoredItem(subscription.DefaultItem)
            {
                StartNodeId = nodeId,
                AttributeId = Attributes.Value,
                MonitoringMode = MonitoringMode.Reporting,
                SamplingInterval = 1000,
                QueueSize = 1,
                DiscardOldest = true
            };
            monitoredItem.Notification += new MonitoredItemNotificationEventHandler(callback);
            subscription.AddItem(monitoredItem);
            await subscription.ApplyChangesAsync(ct);
        }
    }
}
