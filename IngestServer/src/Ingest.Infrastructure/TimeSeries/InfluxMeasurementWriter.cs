using Ingest.Application.Nodes;
using Ingest.Application.Output;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;

namespace Ingest.Infrastructure.TimeSeries;

public sealed class InfluxMeasurementWriter : IMeasurementWriter
{
    private readonly IInfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;

    public InfluxMeasurementWriter(IInfluxDBClient client, string bucket, string org)
    {
        _client = client;
        _bucket = bucket;
        _org = org;
    }

    public async Task WriteAsync(
        CachedNode node,
        DecodedMessage.MeasurementMessage measurement,
        UplinkContext ctx,
        CancellationToken ct = default)
    {
        var points = MeasurementPointBuilder.Build(node, measurement, ctx);
        if (points.Count == 0)
        {
            return;
        }

        var writeApi = _client.GetWriteApiAsync();
        await writeApi.WritePointsAsync([.. points], _bucket, _org, ct);
    }
}