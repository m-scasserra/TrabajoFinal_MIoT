using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;

namespace Ingest.Core.Tests.Decoding;

public class RegisterValueTests
{
    private static RegisterType Type(int regCount, bool signed, string type = "int") => new()
    {
        RegCount = regCount,
        ByteWidth = regCount * 2,
        Signed = signed,
        Type = type,
    };

    [Fact]
    public void U16_Unsigned()
    {
        var v = RegisterValue.Interpret([0x08, 0xFC], Type(1, signed: false));
        Assert.Equal(ValueKind.Unsigned, v.Kind);
        Assert.Equal(2300UL, v.AsUnsigned);
    }

    [Fact]
    public void U16_MaxValue()
    {
        var v = RegisterValue.Interpret([0xFF, 0xFF], Type(1, signed: false));
        Assert.Equal(65535UL, v.AsUnsigned);
    }

    [Fact]
    public void U32_Unsigned()
    {
        var v = RegisterValue.Interpret([0x00, 0x0F, 0x42, 0x40], Type(2, signed: false));
        Assert.Equal(1_000_000UL, v.AsUnsigned);
    }

    [Fact]
    public void U64_Unsigned_ValueHigherThanLongMax()
    {
        var v = RegisterValue.Interpret(
            [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,], Type(4, signed: false));
        Assert.Equal(ulong.MaxValue, v.AsUnsigned);
    }

    [Fact]
    public void I16_Negative()
    {
        var v = RegisterValue.Interpret([0xFF, 0xFF], Type(1, signed: true));
        Assert.Equal(ValueKind.Signed, v.Kind);
        Assert.Equal(-1L, v.AsSigned);
    }

    [Fact]
    public void I16_MinAndMaxValue()
    {
        Assert.Equal(-32768L, RegisterValue.Interpret([0x80, 0x00], Type(1, signed: true)).AsSigned);
        Assert.Equal(32767L, RegisterValue.Interpret([0x7F, 0xFF], Type(1, signed: true)).AsSigned);
    }

    [Fact]
    public void I32_Negative()
    {
        var v = RegisterValue.Interpret([0xFF, 0xFE, 0x11, 0xDC], Type(2, signed: true));
        Assert.Equal(-126500L, v.AsSigned);
    }

    [Fact]
    public void I64_Negative()
    {
        var v = RegisterValue.Interpret(
            [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF], Type(4, signed: true));
        Assert.Equal(-1L, v.AsSigned);
    }

    [Fact]
    public void Float32_KnownValue()
    {
        var v = RegisterValue.Interpret([0x43, 0x66, 0x80, 0x00], Type(2, signed: false, type: "float"));
        Assert.Equal(ValueKind.Float, v.Kind);
        Assert.Equal(230.5f, v.AsFloat, precision: 10);
    }

    [Fact]
    public void Float32_IgnoresSignedFlag()
    {
        var signedType = Type(2, signed: true, type: "float");
        var v = RegisterValue.Interpret([0x43, 0x66, 0x80, 0x00], signedType);
        Assert.Equal(ValueKind.Float, v.Kind);
        Assert.Equal(230.5f, v.AsFloat, precision: 10);
    }

    [Fact]
    public void Float64_KnownValue()
    {
        var v = RegisterValue.Interpret(
            [0x3F, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            Type(4, signed: false, type: "float"));
        Assert.Equal(ValueKind.Float, v.Kind);
        Assert.Equal(1.5f, v.AsFloat, precision: 10);
    }

    [Fact]
    public void Float_InvalidWidth_ThrowsPayloadFormatException()
    {
        Assert.Throws<PayloadFormatException>(() =>
        {
            var v = RegisterValue.Interpret([0x3F, 0x00], Type(1, signed: false, type: "float"));
        });
    }

}
