using Ingest.Core.Config;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Decoding;

public static class PayloadDecoder
{
    public static DecodeResult Decode(ReadOnlySpan<byte> payload, NodeConfig? config)
    {
        if (payload.Length == 0)
        {
            return DecodeResult.Failure(DecodeError.Empty, "Payload is empty.");
        }

        try
        {
            var reader = new PayloadReader(payload);
            var header = Header.Parse(ref reader);

            return header.Type switch
            {
                MessageType.Heartbeat => DecodeHeartbeat(ref reader, header),
                MessageType.Alarm => DecodeAlarm(ref reader, header),
                MessageType.ConfigAck => DecodeConfigAck(ref reader, header),
                MessageType.ConfigReport => DecodeConfigReport(ref reader, header),
                MessageType.Measurement or MessageType.MeasurementBacklog =>
                    DecodeMeasurement(ref reader, header, config),
                _ => DecodeResult.Failure(
                    DecodeError.UnsupportedMessageType,
                    $"Unsupported message type: {header.Type} (0x{(byte)header.Type:X1})."),
            };
        }
        catch (PayloadFormatException ex)
        {
            return DecodeResult.Failure(DecodeError.Malformed, ex.Message);
        }
    }

    private static DecodeResult DecodeHeartbeat(ref PayloadReader reader, Header header)
    {
        var hb = Heartbeat.Decode(ref reader);
        return DecodeResult.Success(
            new DecodedMessage.HeartbeatMessage(hb) { ProtocolVersion = header.Version });
    }

    private static DecodeResult DecodeAlarm(ref PayloadReader reader, Header header)
    {
        var alarm = Alarm.Decode(ref reader);
        return DecodeResult.Success(
            new DecodedMessage.AlarmMessage(alarm) { ProtocolVersion = header.Version });
    }

    private static DecodeResult DecodeConfigAck(ref PayloadReader reader, Header header)
    {
        var ack = ConfigAck.Decode(ref reader);
        return DecodeResult.Success(
            new DecodedMessage.ConfigAckMessage(ack) { ProtocolVersion = header.Version });
    }

    private static DecodeResult DecodeConfigReport(ref PayloadReader reader, Header header)
    {
        var report = ConfigReport.Decode(ref reader);
        return DecodeResult.Success(
            new DecodedMessage.ConfigReportMessage(report) { ProtocolVersion = header.Version });
    }

    private static DecodeResult DecodeMeasurement(
        ref PayloadReader reader, Header header, NodeConfig? config)
    {
        if (config is null)
        {
            return DecodeResult.Failure(
                DecodeError.MissingConfig,
                $"Measurement {header.Type} configuration is missing.");
        }

        var measHeader = MeasurementHeader.Decode(ref reader);
        var samples = MeasurementDecoder.Decode(ref reader, measHeader, header.Type, config);

        return DecodeResult.Success(new DecodedMessage.MeasurementMessage(
            measHeader,
            IsBacklog: header.Type == MessageType.MeasurementBacklog,
            Samples: samples)
        {
            ProtocolVersion = header.Version,
        });
    }

}