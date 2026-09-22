using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol.Messages;
using Ingest.Infrastructure.TimeSeries;

namespace Ingest.Infrastructure.Tests.TimeSeries;

public class HealthPointBuilderTests
{
    private static CachedNode Node(Guid? orgId = null) => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        DevEui = "AABBCCDD00112233",
        Alias = "node",
        OrgId = orgId,
        OperativeState = NodeState.Active,
        Config = new NodeConfig { Schema = 1, Mode = AcquisitionMode.ATM90E32AS, ConfigVersion = 7 },
    };

    private static UplinkContext Ctx(DateTimeOffset received) => new()
    {
        DevEui = "AABBCCDD00112233",
        Payload = [],
        FrameCounter = 1,
        ReceivedAt = received,
    };

    private static DecodedMessage.HeartbeatMessage Heartbeat(
        byte battery, ushort bootCount, bool mains, bool backlog)
    {
        var hb = new Heartbeat(
            ConfigVersion: 7,
            FwMajor: 2, FwMinor: 3,
            BatteryPercent: battery,
            BootCount: bootCount,
            MainsPresent: mains,
            BacklogNotEmpty: backlog,
            RawFlags: (byte)((mains ? 1 : 0) | (backlog ? 2 : 0)));

        return new DecodedMessage.HeartbeatMessage(hb) { ProtocolVersion = 1 };
    }

    [Fact]
    public void Build_GeneratePointWithAllFields()
    {
        var node = Node(orgId: Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var received = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var msg = Heartbeat(battery: 87, bootCount: 258, mains: true, backlog: false);

        var point = HealthPointBuilder.Build(node, msg, Ctx(received));
        string line = point.ToLineProtocol();

        Assert.StartsWith("node_health", line);
        Assert.Contains("dev_eui=AABBCCDD00112233", line);
        Assert.Contains("org_id=22222222-2222-2222-2222-222222222222", line);
        Assert.Contains("battery_percent=87", line);
        Assert.Contains("boot_count=258", line);
        Assert.Contains("config_version=7", line);
        Assert.Contains("fw_major=2", line);
        Assert.Contains("fw_minor=3", line);
        Assert.Contains("mains_present=true", line);
        Assert.Contains("backlog_not_empty=false", line);
    }

    [Fact]
    public void Build_RisedFlags_Reflects()
    {
        var node = Node();
        var msg = Heartbeat(battery: 50, bootCount: 10, mains: false, backlog: true);

        var point = HealthPointBuilder.Build(node, msg, Ctx(DateTimeOffset.UtcNow));
        string line = point.ToLineProtocol();

        Assert.Contains("mains_present=false", line);
        Assert.Contains("backlog_not_empty=true", line);
    }

    [Fact]
    public void Build_NoOrgId_NotIncluded()
    {
        var node = Node(orgId: null);
        var msg = Heartbeat(battery: 50, bootCount: 10, mains: true, backlog: false);

        string line = HealthPointBuilder.Build(node, msg, Ctx(DateTimeOffset.UtcNow)).ToLineProtocol();

        Assert.DoesNotContain("org_id=", line);
    }
}