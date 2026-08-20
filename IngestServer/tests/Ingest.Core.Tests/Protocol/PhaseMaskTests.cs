using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Protocol;

public class PhaseMaskTests
{
    [Theory]
    [InlineData(0x0F, true, true, true, 3)]
    [InlineData(0x0E, true, true, true, 3)]
    [InlineData(0x02, true, false, false, 1)]
    [InlineData(0x03, true, false, false, 1)]
    [InlineData(0x06, true, true, false, 2)]
    [InlineData(0x0C, false, true, true, 2)]
    [InlineData(0x08, false, false, true, 1)]
    public void Bits_PhaseMaskApplies(
        byte raw, bool l1, bool l2, bool l3, int count
    )
    {
        var mask = PhaseMask.FromByte(raw);
        Assert.Equal(l1, mask.HasL1);
        Assert.Equal(l2, mask.HasL2);
        Assert.Equal(l3, mask.HasL3);
        Assert.Equal(count, mask.ActivePhaseCount);
    }

    [Fact]
    public void ActivePhases_ReturnsPhasesInOrder()
    {
        var mas = PhaseMask.FromByte(0x0F);
        Assert.Equal(new[] { PhaseTag.L1, PhaseTag.L2, PhaseTag.L3 }, mas.ActivePhases());
    }

    [Fact]
    public void ActivePhases_L2L3InOrder()
    {
        var mas = PhaseMask.FromByte(0x0C);
        Assert.Equal(new[] { PhaseTag.L2, PhaseTag.L3 }, mas.ActivePhases());
    }
}