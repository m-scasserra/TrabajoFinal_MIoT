using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;

namespace Ingest.Application.Output;

public interface IMeasurementWriter
{
    Task WriteAsync(
        CachedNode node,
        DecodedMessage.MeasurementMessage measurement,
        UplinkContext ctx,
        CancellationToken ct = default);
}