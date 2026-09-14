namespace Ingest.Core.Decoding;

public sealed class MeasurementSample
{
    public required ushort SeqNumber { get; init; }
    public required MeasurementData Data { get; init; }
}