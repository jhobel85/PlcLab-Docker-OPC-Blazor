using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;

namespace PlcLab.Application.Ports
{
    public interface IReadWritePort
    {
        Task<object> ReadValueAsync(Session session, NodeId nodeId, CancellationToken ct = default);
        Task WriteValueAsync(Session session, NodeId nodeId, Variant value, CancellationToken ct = default);
    }
}
