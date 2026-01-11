using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;

namespace PlcLab.Application.Ports
{
    public interface IBrowsePort
    {
        Task<NodeId> ResolveNodeIdAsync(Session session, string path, CancellationToken ct = default);
        Task<ReferenceDescriptionCollection> BrowseAsync(Session session, NodeId nodeId, CancellationToken ct = default);
    }
}
