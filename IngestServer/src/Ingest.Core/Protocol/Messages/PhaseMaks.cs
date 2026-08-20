using Ingest.Core.Protocol;

namespace Ingest.Core.Protocol.Messages;

public readonly record struct PhaseMask(byte Raw)
{
    private const byte L1Bit = 0b0000_0010;
    private const byte L2Bit = 0b0000_0100;
    private const byte L3Bit = 0b0000_1000;

    public bool HasL1 => (Raw & L1Bit) != 0;
    public bool HasL2 => (Raw & L2Bit) != 0;
    public bool HasL3 => (Raw & L3Bit) != 0;

    public int ActivePhaseCount =>
        (HasL1 ? 1 : 0) + (HasL2 ? 1 : 0) + (HasL3 ? 1 : 0);


    public IEnumerable<PhaseTag> ActivePhases()
    {
        if (HasL1) yield return PhaseTag.L1;
        if (HasL2) yield return PhaseTag.L2;
        if (HasL3) yield return PhaseTag.L3;
    }

    public static PhaseMask FromByte(byte value) => new(value);
}