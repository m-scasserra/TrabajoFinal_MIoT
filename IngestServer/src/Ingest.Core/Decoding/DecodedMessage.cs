using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Decoding;

public abstract record DecodedMessage
{
    public required byte ProtocolVersion { get; init; }
    public sealed record HeartbeatMessage(Heartbeat Heartbeat) : DecodedMessage;
    public sealed record AlarmMessage(Alarm Alarm) : DecodedMessage;
    public sealed record ConfigAckMessage(ConfigAck ConfigAck) : DecodedMessage;
    public sealed record ConfigReportMessage(ConfigReport ConfigReport) : DecodedMessage;
    public sealed record MeasurementMessage(
        MeasurementHeader Header,
        bool IsBacklog,
        IReadOnlyList<MeasurementSample> Samples) : DecodedMessage;
}