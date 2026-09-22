using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace Ingest.Infrastructure.TimeSeries;

public static class HealthPointBuilder
{
    private const string Measurement = "node_health";

    public static PointData Build(
        CachedNode node,
        DecodedMessage.HeartbeatMessage message,
        UplinkContext ctx)
    {
        var hb = message.Heartbeat;

        var point = PointData
            .Measurement(Measurement)
            .Tag("dev_eui", node.DevEui)
            .Tag("node_id", node.Id.ToString())
            .Field("battery_percent", hb.BatteryPercent)
            .Field("boot_count", hb.BootCount)
            .Field("config_version", hb.ConfigVersion)
            .Field("fw_major", hb.FwMajor)
            .Field("fw_minor", hb.FwMinor)
            .Field("mains_present", hb.MainsPresent)
            .Field("backlog_not_empty", hb.BacklogNotEmpty)
            .Timestamp(ctx.ReceivedAt.UtcDateTime, WritePrecision.Ns);

        if (node.OrgId is { } orgId)
        {
            point = point.Tag("org_id", orgId.ToString());
        }

        return point;
    }
}