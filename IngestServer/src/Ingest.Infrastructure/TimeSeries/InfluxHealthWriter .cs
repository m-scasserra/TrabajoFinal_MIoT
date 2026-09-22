using Ingest.Application.Nodes;
using Ingest.Application.Output;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;
using InfluxDB.Client;

namespace Ingest.Infrastructure.TimeSeries;

public sealed class InfluxHealthWriter : IHealthWriter
{
    private readonly IInfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;

    public InfluxHealthWriter(IInfluxDBClient client, string bucket, string org)
    {
        _client = client;
        _bucket = bucket;
        _org = org;
    }

    public async Task WriteHeartbeatAsync(
        CachedNode node,
        DecodedMessage.HeartbeatMessage heartbeat,
        UplinkContext ctx,
        CancellationToken ct = default)
    {
        var point = HealthPointBuilder.Build(node, heartbeat, ctx);
        var writeApi = _client.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _bucket, _org, ct);
    }
}