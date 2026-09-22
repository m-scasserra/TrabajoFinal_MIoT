using Ingest.Application.Nodes;
using Ingest.Application.Output;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;

namespace Ingest.Application.Tests.Pipeline;

public sealed class FakeMeasurementWriter : IMeasurementWriter
{
    public int Writes { get; private set; }
    public DecodedMessage.MeasurementMessage? Last { get; private set; }

    public Task WriteAsync(CachedNode node, DecodedMessage.MeasurementMessage measurement,
        UplinkContext ctx, CancellationToken ct = default)
    {
        Writes++;
        Last = measurement;
        return Task.CompletedTask;
    }
}

public sealed class FakeHealthWriter : IHealthWriter
{
    public int Writes { get; private set; }

    public Task WriteHeartbeatAsync(CachedNode node, DecodedMessage.HeartbeatMessage heartbeat,
        UplinkContext ctx, CancellationToken ct = default)
    {
        Writes++;
        return Task.CompletedTask;
    }
}

public sealed class FakeBackendPublisher : IBackendPublisher
{
    public int Alarms { get; private set; }
    public int Acks { get; private set; }
    public int Reports { get; private set; }
    public int Desyncs { get; private set; }
    public byte? LastDesyncVersion { get; private set; }

    public Task PublishAlarmAsync(CachedNode node, DecodedMessage.AlarmMessage alarm,
        UplinkContext ctx, CancellationToken ct = default)
    {
        Alarms++;
        return Task.CompletedTask;
    }

    public Task PublishConfigAckAsync(CachedNode node, DecodedMessage.ConfigAckMessage ack,
        UplinkContext ctx, CancellationToken ct = default)
    {
        Acks++;
        return Task.CompletedTask;
    }

    public Task PublishConfigReportAsync(CachedNode node, DecodedMessage.ConfigReportMessage report,
        UplinkContext ctx, CancellationToken ct = default)
    {
        Reports++;
        return Task.CompletedTask;
    }

    public Task PublishDesyncAsync(CachedNode node, byte payloadConfigVersion,
        UplinkContext ctx, CancellationToken ct = default)
    {
        Desyncs++;
        LastDesyncVersion = payloadConfigVersion;
        return Task.CompletedTask;
    }
}