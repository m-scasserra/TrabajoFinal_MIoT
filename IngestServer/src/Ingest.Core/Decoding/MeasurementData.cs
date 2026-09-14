using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Decoding;

public readonly record struct MeasurementData
{
    public required PhaseMask PhaseMask { get; init; }
    public required IReadOnlyList<MeasurementReading> Readings { get; init; }
}