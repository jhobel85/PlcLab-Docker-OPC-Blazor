using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.Application.Ports;
using Serilog;

namespace PlcLab.Infrastructure
{
    public class BrowseService(IConfiguration configuration, IBrowsePort browsePort)
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IBrowsePort _browsePort = browsePort;

        // Recursively browse all nodes and log their details        
        public async Task RecursiveBrowseAsync(Session session, NodeId nodeId, CancellationToken ct, string indent)
        {
            var children = await _browsePort.BrowseAsync(session, nodeId, ct);
            foreach (var child in children)
            {
                Log.Information("{Indent}- DisplayName: {DisplayName}, BrowseName: {BrowseName}, NodeId: {NodeId}, NodeClass: {NodeClass}",
                    indent, child.DisplayName.Text, child.BrowseName.ToString(), child.NodeId, child.NodeClass);
                // Recurse into child nodes that are Objects or Folders
                if (child.NodeClass == NodeClass.Object || child.NodeClass == NodeClass.Variable || child.NodeClass == NodeClass.Method)
                {
                    var childNodeId = ExpandedNodeId.ToNodeId(child.NodeId, session.NamespaceUris);
                    await RecursiveBrowseAsync(session, childNodeId, ct, indent + "  ");
                }
            }
        }

        // Helper to get parent node of a given node by browsing inverse references
        public async Task<NodeId> GetParentNodeIdAsync(Session session, NodeId nodeId)
        {
            var refs = await session.FetchReferencesAsync(nodeId).ConfigureAwait(false);
            var parentRef = refs.FirstOrDefault(r => r.ReferenceTypeId == ReferenceTypeIds.HasComponent && r.IsForward == false) ?? throw new Exception("Parent node not found for method node.");
            return ExpandedNodeId.ToNodeId(parentRef.NodeId, session.NamespaceUris);
        }
    }
}
