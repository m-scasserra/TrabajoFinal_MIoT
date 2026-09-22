namespace Ingest.Application.Pipeline;

public sealed class UplinkContext
{
    public required string DevEui { get; init; }
    public required byte[] Payload { get; init; }
    public required uint FrameCounter { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }

    public double? Rssi { get; init; }

    public double? Snr { get; init; }
}