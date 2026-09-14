using Ingest.Core.Protocol;

namespace Ingest.Core.Decoding;

public readonly record struct MeasurementReading(
    string Measurand,
    PhaseTag Phase,
    MeasurementValue Value,
    double Scale,
    string? Unit)
{
    public double PhysicalValue => Value.ToDouble() * Scale;
}