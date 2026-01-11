using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.Application.Ports;

namespace PlcLab.OPC.Adapters
{
    public class OpcBrowseAdapter : IBrowsePort
    {
        public async Task<NodeId> ResolveNodeIdAsync(Session session, string path, CancellationToken ct = default)
        {
            if (NodeId.TryParse(path, out var parsedNodeId))
            {
                return parsedNodeId;
            }
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentId = ObjectIds.RootFolder;
            foreach (var part in parts)
            {
                var browseResult = await BrowseAsync(session, currentId, ct);
                var reference = browseResult.FirstOrDefault(r => r.DisplayName.Text == part);
                if (reference == null)
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, $"Node '{part}' not found in path '{path}'");
                currentId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
            }
            return currentId;
        }

        public async Task<ReferenceDescriptionCollection> BrowseAsync(Session session, NodeId nodeId, CancellationToken ct = default)
        {
            var browseDescription = new BrowseDescription
            {
                NodeId = nodeId,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask = (uint)NodeClass.Object | (uint)NodeClass.Variable | (uint)NodeClass.Method,
                ResultMask = (uint)BrowseResultMask.All
            };
            var browseResponse = await session.BrowseAsync(null, null, 0, new BrowseDescriptionCollection { browseDescription }, ct);
            if (StatusCode.IsBad(browseResponse.Results[0].StatusCode))
                throw new ServiceResultException(browseResponse.Results[0].StatusCode);
            return browseResponse.Results[0].References;
        }
    }
}
