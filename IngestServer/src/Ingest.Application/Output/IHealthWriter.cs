using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;

namespace Ingest.Application.Output;

public interface IHealthWriter
{
    Task WriteHeartbeatAsync(
        CachedNode node,
        DecodedMessage.HeartbeatMessage heartbeat,
        UplinkContext ctx,
        CancellationToken ct = default);
}