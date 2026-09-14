namespace Ingest.Core.Decoding;

public enum ValueKind : byte
{
    Signed,
    Unsigned,
    Float,
}

public readonly record struct MeasurementValue
{
    public ValueKind Kind { get; }

    private readonly long _signed;
    private readonly ulong _unsigned;
    private readonly double _float;

    private MeasurementValue(ValueKind kind, long s, ulong u, double f)
    {
        Kind = kind;
        _signed = s;
        _unsigned = u;
        _float = f;
    }

    public static MeasurementValue FromSigned(long value) => new(ValueKind.Signed, value, 0, 0);
    public static MeasurementValue FromUnsigned(ulong value) => new(ValueKind.Unsigned, 0, value, 0);
    public static MeasurementValue FromFloat(double value) => new(ValueKind.Float, 0, 0, value);

    public long AsSigned => Kind == ValueKind.Signed
        ? _signed
        : throw new InvalidOperationException($"Cannot convert {Kind} to signed.");

    public ulong AsUnsigned => Kind == ValueKind.Unsigned
        ? _unsigned
        : throw new InvalidOperationException($"Cannot convert {Kind} to unsigned.");

    public double AsFloat => Kind == ValueKind.Float
        ? _float
        : throw new InvalidOperationException($"Cannot convert {Kind} to float.");

    public double ToDouble() => Kind switch
    {
        ValueKind.Signed => _signed,
        ValueKind.Unsigned => _unsigned,
        ValueKind.Float => _float,
        _ => throw new InvalidOperationException($"Unknown kind: {Kind}.")
    };

    public override string ToString() => Kind switch
    {
        ValueKind.Signed => _signed.ToString(),
        ValueKind.Unsigned => _unsigned.ToString(),
        ValueKind.Float => _float.ToString(),
        _ => "?",
    };
}