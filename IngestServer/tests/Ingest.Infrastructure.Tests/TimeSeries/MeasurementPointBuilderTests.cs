using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;
using Ingest.Infrastructure.TimeSeries;
using InfluxDB.Client.Api.Domain;

namespace Ingest.Infrastructure.Tests.TimeSeries;

public class MeasurementPointBuilderTests
{
    private static CachedNode Node(Guid? orgId = null) => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        DevEui = "AABBCCDD00112233",
        Alias = "node",
        OrgId = orgId,
        OperativeState = NodeState.Active,
        Config = new NodeConfig { Schema = 1, Mode = AcquisitionMode.ATM90E32AS, ConfigVersion = 3 },
    };

    private static UplinkContext Ctx(DateTimeOffset received) => new()
    {
        DevEui = "AABBCCDD00112233",
        Payload = [],
        FrameCounter = 1,
        ReceivedAt = received,
    };

    private static DecodedMessage.MeasurementMessage LiveMessage(params MeasurementReading[] readings)
    {
        var data = new MeasurementData
        {
            PhaseMask = PhaseMask.FromByte(0x02),
            Readings = readings,
        };
        var sample = new MeasurementSample { SeqNumber = 500, Data = data };
        return new DecodedMessage.MeasurementMessage(
            new MeasurementHeader(3, MeasurementFlags.FromByte(0), 500),
            IsBacklog: false,
            Samples: [sample])
        { ProtocolVersion = 1 };
    }

    [Fact]
    public void Live_OnePhase_GeneratesMeasurementPointWithScaledFields()
    {
        var node = Node(orgId: Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var received = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        var msg = LiveMessage(
            new MeasurementReading("voltage_rms", PhaseTag.L1, MeasurementValue.FromUnsigned(2300), 0.1, "V"),
            new MeasurementReading("current_rms", PhaseTag.L1, MeasurementValue.FromUnsigned(550), 0.01, "A"));

        var points = MeasurementPointBuilder.Build(node, msg, Ctx(received));

        Assert.Single(points);
        string line = points[0].ToLineProtocol();

        Assert.StartsWith("electrical", line);
        Assert.Contains("dev_eui=AABBCCDD00112233", line);
        Assert.Contains("phase=L1", line);
        Assert.Contains("backlog=false", line);
        Assert.Contains("org_id=22222222-2222-2222-2222-222222222222", line);

        Assert.Contains("voltage_rms=230", line);
        Assert.Contains("current_rms=5.5", line);
        Assert.Contains("seq_number=500", line);
    }

    [Fact]
    public void Live_ThreePhase_GeneratesOnePointPerPhase()
    {
        var node = Node();
        var msg = LiveMessage(
            new MeasurementReading("voltage_rms", PhaseTag.L1, MeasurementValue.FromUnsigned(2300), 0.1, "V"),
            new MeasurementReading("voltage_rms", PhaseTag.L2, MeasurementValue.FromUnsigned(2278), 0.1, "V"),
            new MeasurementReading("voltage_rms", PhaseTag.L3, MeasurementValue.FromUnsigned(2320), 0.1, "V"));

        var points = MeasurementPointBuilder.Build(node, msg, Ctx(DateTimeOffset.UtcNow));

        Assert.Equal(3, points.Count);
        var lines = points.Select(p => p.ToLineProtocol()).ToList();
        Assert.Contains(lines, l => l.Contains("phase=L1") && l.Contains("voltage_rms=230"));
        Assert.Contains(lines, l => l.Contains("phase=L2") && l.Contains("voltage_rms=227.8"));
        Assert.Contains(lines, l => l.Contains("phase=L3") && l.Contains("voltage_rms=232"));
    }

    [Fact]
    public void NoOrgId_NullTagOrg()
    {
        var node = Node(orgId: null);
        var msg = LiveMessage(
            new MeasurementReading("voltage_rms", PhaseTag.L1, MeasurementValue.FromUnsigned(2300), 0.1, "V"));

        var points = MeasurementPointBuilder.Build(node, msg, Ctx(DateTimeOffset.UtcNow));

        Assert.DoesNotContain("org_id=", points[0].ToLineProtocol());
    }

    [Fact]
    public void Backlog_RaisesFlagAndTagsBySeq()
    {
        var node = Node();
        var received = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        var data1 = new MeasurementData
        {
            PhaseMask = PhaseMask.FromByte(0x02),
            Readings = [new MeasurementReading("pulse_count", PhaseTag.L1, MeasurementValue.FromUnsigned(1000), 1.0, "pulses")],
        };
        var data2 = new MeasurementData
        {
            PhaseMask = PhaseMask.FromByte(0x02),
            Readings = [new MeasurementReading("pulse_count", PhaseTag.L1, MeasurementValue.FromUnsigned(2000), 1.0, "pulses")],
        };
        var msg = new DecodedMessage.MeasurementMessage(
            new MeasurementHeader(7, MeasurementFlags.FromByte(0x01), 100),
            IsBacklog: true,
            Samples:
            [
                new MeasurementSample {SeqNumber = 100, Data = data1},
                new MeasurementSample {SeqNumber = 101, Data = data2},
            ])
        { ProtocolVersion = 1 };

        var points = MeasurementPointBuilder.Build(node, msg, Ctx(received));

        Assert.Equal(2, points.Count);
        var lines = points.Select(p => p.ToLineProtocol()).ToList();

        Assert.All(lines, l => Assert.Contains("backlog=true", l));
        Assert.NotEqual(
            lines[0].Split(' ').Last(),
            lines[1].Split(' ').Last());
    }
}