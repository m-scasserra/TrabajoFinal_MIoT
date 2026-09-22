using System.Globalization;
using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Application.Tests.Nodes;
using Ingest.Core.Config;
using Ingest.Core.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ingest.Application.Tests.Pipeline;

public class UplinkProcessorTests
{
    private const string DevEui = "AABBCCDD00112233";

    private sealed class Harness
    {
        public FakeNodeRepository Repo { get; } = new();
        public FakeMeasurementWriter Measurements { get; } = new();
        public FakeHealthWriter Health { get; } = new();
        public FakeBackendPublisher Backend { get; } = new();
        public InMemoryDeduplicationStore Dedup { get; } = new();
        public UplinkProcessor Processor { get; }

        public Harness()
        {
            var cache = new NodeCache(Repo, NullLogger<NodeCache>.Instance);
            Processor = new UplinkProcessor(
                cache, Dedup, Measurements, Health, Backend,
                NullLogger<UplinkProcessor>.Instance);
        }
    }

    private static CachedNode PulseNode(byte configVersion) => new()
    {
        Id = Guid.NewGuid(),
        DevEui = DevEui,
        Alias = "node",
        OperativeState = NodeState.Active,
        Config = new NodeConfig
        {
            Schema = 1,
            Mode = AcquisitionMode.Pulse,
            ConfigVersion = configVersion,
            PhaseConfig = new PhaseConfig
            {
                RawByte = 0x02,
                ThreePhase = false,
                PhaseBits = 1,
                ActivePhases = [PhaseTag.L1],
            },
            PulseCounters = [new PulseCounter { Phase = PhaseTag.L1, PulsesPerKwh = 1000 }],
        }
    };

    private static UplinkContext Ctx(byte[] payload, uint fCnt = 1) => new()
    {
        DevEui = DevEui,
        Payload = payload,
        FrameCounter = fCnt,
        ReceivedAt = DateTimeOffset.UtcNow,
    };

    private static byte[] MeasurementV7() =>
        [0x11, 0x07, 0x00, 0x01, 0xF4, 0x02, 0x00, 0x00, 0x03, 0xE8];

    [Fact]
    public async Task Measurement_Consistent_Writes()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 7));

        var outcome = await h.Processor.ProcessAsync(Ctx(MeasurementV7()));

        Assert.Equal(ProcessOutcome.Processed, outcome);
        Assert.Equal(1, h.Measurements.Writes);
        Assert.Equal(0, h.Backend.Desyncs);
    }

    [Fact]
    public async Task Measurement_Desync_DoesNotWriteAndNotifies()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 8));

        var outcome = await h.Processor.ProcessAsync(Ctx(MeasurementV7()));

        Assert.Equal(ProcessOutcome.Desynchronized, outcome);
        Assert.Equal(0, h.Measurements.Writes);
        Assert.Equal(1, h.Backend.Desyncs);
        Assert.Equal<byte?>(7, h.Backend.LastDesyncVersion);
    }

    [Fact]
    public async Task UnkownDevice_Ignores()
    {
        var h = new Harness();

        var outcome = await h.Processor.ProcessAsync(Ctx(MeasurementV7()));

        Assert.Equal(ProcessOutcome.UnknownDevice, outcome);
        Assert.Equal(0, h.Measurements.Writes);
    }

    [Fact]
    public async Task Duplicated_IgnoresNotModifingNodesOrOutput()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 7));

        var first = await h.Processor.ProcessAsync(Ctx(MeasurementV7(), fCnt: 42));
        var second = await h.Processor.ProcessAsync(Ctx(MeasurementV7(), fCnt: 42));

        Assert.Equal(ProcessOutcome.Processed, first);
        Assert.Equal(ProcessOutcome.Duplicate, second);
        Assert.Equal(1, h.Measurements.Writes);
    }

    [Fact]
    public async Task Alarm_SendBackend()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 7));

        byte[] payload = [0x13, 0x07, 0x01, 0x2C, 0x01, 0x02, 0x08, 0xFC];
        var outcome = await h.Processor.ProcessAsync(Ctx(payload));

        Assert.Equal(ProcessOutcome.Processed, outcome);
        Assert.Equal(1, h.Backend.Alarms);
        Assert.Equal(0, h.Measurements.Writes);
    }

    [Fact]
    public async Task Heartbeat_WritesInHealth()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 7));

        byte[] payload = [0x00, 0x07, 0x23, 0x57, 0x01, 0x02, 0x03];
        var outcome = await h.Processor.ProcessAsync(Ctx(payload));

        Assert.Equal(ProcessOutcome.Processed, outcome);
        Assert.Equal(1, h.Health.Writes);
    }

    [Fact]
    public async Task ConfigAck_SendToBackend()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 7));

        byte[] payload = [0x14, 0x0B, 0x00, 0x04, 0x9F, 0x3A];
        var outcome = await h.Processor.ProcessAsync(Ctx(payload));

        Assert.Equal(ProcessOutcome.Processed, outcome);
        Assert.Equal(1, h.Backend.Acks);
    }

    [Fact]
    public async Task PayloadMalformed_Ignores()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 7));

        byte[] payload = [0x11, 0x07, 0x00, 0x01, 0xF4, 0x02, 0x00, 0x00];
        var outcome = await h.Processor.ProcessAsync(Ctx(payload));

        Assert.Equal(ProcessOutcome.Malformed, outcome);
        Assert.Equal(0, h.Health.Writes);
    }

    [Fact]
    public async Task Downlink_AsUplink_Unsupported()
    {
        var h = new Harness();
        h.Repo.Set(PulseNode(configVersion: 7));

        byte[] payload = [0x18, 0x01, 0x05, 0x02];
        var outcome = await h.Processor.ProcessAsync(Ctx(payload));

        Assert.Equal(ProcessOutcome.Unsupported, outcome);
    }
}
