using Ingest.Core.Decoding;

namespace Ingest.Core.Tests.Decoding;

public class MeasurementValueTests
{
    [Fact]
    public void Signed_ExposesAsSignedAndConvertsToDouble()
    {
        var v = MeasurementValue.FromSigned(-126500);
        Assert.Equal(ValueKind.Signed, v.Kind);
        Assert.Equal(-126500, v.AsSigned);
        Assert.Equal(-126500.0, v.ToDouble(), precision: 6);
    }

    [Fact]
    public void Unsigned_SupportsValuesHigherThanLongMax()
    {
        ulong big = ulong.MaxValue;
        var v = MeasurementValue.FromUnsigned(big);

        Assert.Equal(ValueKind.Unsigned, v.Kind);
        Assert.Equal(big, v.AsUnsigned);
    }

    [Fact]
    public void Float_ExposesAsFloat()
    {
        var v = MeasurementValue.FromFloat(230.5);
        Assert.Equal(ValueKind.Float, v.Kind);
        Assert.Equal(230.5, v.AsFloat, precision: 6);
        Assert.Equal(230.5, v.ToDouble(), precision: 6);
    }

    [Fact]
    public void IncorrectAccesor_ThrowsInvalidOperationException()
    {
        var v = MeasurementValue.FromSigned(10);
        Assert.Throws<InvalidOperationException>(() => v.AsFloat);
        Assert.Throws<InvalidOperationException>(() => v.AsUnsigned);
    }

    [Fact]
    public void Equals_ByValueAndKind()
    {
        Assert.Equal(MeasurementValue.FromSigned(5), MeasurementValue.FromSigned(5));
        Assert.NotEqual(MeasurementValue.FromSigned(5), MeasurementValue.FromUnsigned(5));
    }
}