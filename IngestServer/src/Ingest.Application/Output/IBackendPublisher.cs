using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;

namespace Ingest.Application.Output;

public interface IBackendPublisher
{
    Task PublishAlarmAsync(
        CachedNode node, DecodedMessage.AlarmMessage alarm, UplinkContext ctx, CancellationToken ct = default);

    Task PublishConfigAckAsync(
        CachedNode node, DecodedMessage.ConfigAckMessage cack, UplinkContext ctx, CancellationToken ct = default);

    Task PublishConfigReportAsync(
        CachedNode node, DecodedMessage.ConfigReportMessage report, UplinkContext ctx, CancellationToken ct = default);

    Task PublishDesyncAsync(
        CachedNode node, byte payloadConfigVersion, UplinkContext ctx, CancellationToken ct = default);
}