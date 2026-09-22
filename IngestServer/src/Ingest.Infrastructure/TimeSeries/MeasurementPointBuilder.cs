using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using InfluxDB.Client.Writes;

namespace Ingest.Infrastructure.TimeSeries;

public static class MeasurementPointBuilder
{
    private const string Measurement = "electrical";

    public static IReadOnlyList<PointData> Build(
        CachedNode node,
        DecodedMessage.MeasurementMessage message,
        UplinkContext ctx)
    {
        var points = new List<PointData>();

        foreach (var sample in message.Samples)
        {
            var byPhase = sample.Data.Readings.GroupBy(r => r.Phase);

            foreach (var phaseGroup in byPhase)
            {
                var point = PointData
                .Measurement(Measurement)
                .Tag("dev_eui", node.DevEui)
                .Tag("node_id", node.Id.ToString())
                .Tag("phase", PhaseName(phaseGroup.Key))
                .Tag("backlog", message.IsBacklog ? "true" : "false");

                if (node.OrgId is { } orgId)
                {
                    point = point.Tag("org_id", orgId.ToString());
                }

                foreach (var reading in phaseGroup)
                {
                    point = point.Field(reading.Measurand, reading.PhysicalValue);
                }

                point = point.Field("seq_number", sample.SeqNumber);

                point = point.Timestamp(
                    TimestampFor(ctx.ReceivedAt, message.IsBacklog, sample.SeqNumber),
                    InfluxDB.Client.Api.Domain.WritePrecision.Ns);

                points.Add(point);
            }
        }

        return points;
    }

    private static DateTime TimestampFor(DateTimeOffset receivedAt, bool isBacklog, ushort seq)
    {
        if (!isBacklog)
        {
            return receivedAt.UtcDateTime;
        }

        return receivedAt.UtcDateTime.AddTicks(seq);
    }

    private static string PhaseName(PhaseTag phase) => phase switch
    {
        PhaseTag.L1 => "L1",
        PhaseTag.L2 => "L2",
        PhaseTag.L3 => "L3",
        PhaseTag.Neutral => "N",
        PhaseTag.General => "general",
        _ => "unknown",
    };
}