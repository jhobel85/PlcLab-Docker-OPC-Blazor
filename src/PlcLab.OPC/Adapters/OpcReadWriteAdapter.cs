using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.Application.Ports;

namespace PlcLab.OPC.Adapters
{
    public class OpcReadWriteAdapter : IReadWritePort
    {
        public async Task<object> ReadValueAsync(Session session, NodeId nodeId, CancellationToken ct = default)
        {
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };
            var readResponse = await session.ReadAsync(null, 0, TimestampsToReturn.Neither, new ReadValueIdCollection { readValueId }, ct);
            if (StatusCode.IsBad(readResponse.Results[0].StatusCode))
                throw new ServiceResultException(readResponse.Results[0].StatusCode);
            return readResponse.Results[0].Value;
        }

        public async Task WriteValueAsync(Session session, NodeId nodeId, Variant value, CancellationToken ct = default)
        {
            var writeValue = new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new DataValue { Value = value }
            };
            var writeResponse = await session.WriteAsync(null, new WriteValueCollection { writeValue }, ct);
            if (StatusCode.IsBad(writeResponse.Results[0]))
                throw new ServiceResultException(writeResponse.Results[0]);
        }
    }
}
