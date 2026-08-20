using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Protocol;

public class FragInfoTests
{
    [Theory]
    [InlineData(0x01, 0, 1)]
    [InlineData(0x02, 0, 2)]
    [InlineData(0x12, 1, 2)]
    [InlineData(0x23, 2, 3)]
    public void FromByte_RightValues_Decompose(byte value, byte index, byte total)
    {
        var frag = FragInfo.FromByte(value);
        Assert.Equal(index, frag.Index);
        Assert.Equal(total, frag.Total);
    }

    [Fact]
    public void FromByte_TotalZero_Throw()
    {
        Assert.Throws<PayloadFormatException>(() => FragInfo.FromByte(0x00));
    }

    [Theory]
    [InlineData(0x11)]
    [InlineData(0x21)]
    [InlineData(0x31)]
    public void FromByte_InvalidValues_Throw(byte value)
    {
        Assert.Throws<PayloadFormatException>(() => FragInfo.FromByte(value));
    }

    [Fact]
    public void IsLast_LastFragment_True()
    {
        Assert.True(FragInfo.FromByte(0x12).IsLast);
        Assert.False(FragInfo.FromByte(0x02).IsLast);
    }
}