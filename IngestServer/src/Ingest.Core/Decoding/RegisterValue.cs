using System.Buffers.Binary;
using Ingest.Core.Config;
using Ingest.Core.Protocol;

namespace Ingest.Core.Decoding;

public static class RegisterValue
{
    public static MeasurementValue Interpret(ReadOnlySpan<byte> canonical, RegisterType type)
    {
        if (type.IsFloat)
        {
            return InterpretFloat(canonical);
        }

        return InterpretInteger(canonical, type.Signed);
    }

    private static MeasurementValue InterpretFloat(ReadOnlySpan<byte> canonical)
    {
        return canonical.Length switch
        {
            4 => MeasurementValue.FromFloat(BinaryPrimitives.ReadSingleBigEndian(canonical)),
            8 => MeasurementValue.FromFloat(BinaryPrimitives.ReadDoubleBigEndian(canonical)),
            _ => throw new PayloadFormatException(
                $"Unsupported float size: {canonical.Length} bytes, expected 4 or 8 bytes."),
        };
    }

    private static MeasurementValue InterpretInteger(ReadOnlySpan<byte> canonical, bool signed)
    {
        return canonical.Length switch
        {
            2 => signed
                ? MeasurementValue.FromSigned(BinaryPrimitives.ReadInt16BigEndian(canonical))
                : MeasurementValue.FromUnsigned(BinaryPrimitives.ReadUInt16BigEndian(canonical)),
            4 => signed
                ? MeasurementValue.FromSigned(BinaryPrimitives.ReadInt32BigEndian(canonical))
                : MeasurementValue.FromUnsigned(BinaryPrimitives.ReadUInt32BigEndian(canonical)),
            8 => signed
                ? MeasurementValue.FromSigned(BinaryPrimitives.ReadInt64BigEndian(canonical))
                : MeasurementValue.FromUnsigned(BinaryPrimitives.ReadUInt64BigEndian(canonical)),
            _ => throw new PayloadFormatException(
                $"Unsupported integer size: {canonical.Length} bytes, expected 2, 4, or 8 bytes."),
        };
    }
}