using System.Buffers.Binary;

namespace Ingest.Core.Protocol;

public ref struct PayloadReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    public PayloadReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public readonly int Position => _position;
    public readonly int Remaining => _buffer.Length - _position;

    private void EnsureAvailable(int count)
    {
        if (_position + count > _buffer.Length)
        {
            throw new PayloadFormatException($"Unexpected end of data. Requested {count} bytes, but only {Remaining} available.");
        }
    }

    public byte ReadU8()
    {
        EnsureAvailable(1);
        return _buffer[_position++];
    }

    public ushort ReadU16()
    {
        EnsureAvailable(2);
        ushort value = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Slice(_position, 2));
        _position += 2;
        return value;
    }

    public uint ReadU32()
    {
        EnsureAvailable(4);
        uint value = BinaryPrimitives.ReadUInt32BigEndian(_buffer.Slice(_position, 4));
        _position += 4;
        return value;
    }

    public short ReadI16()
    {
        EnsureAvailable(2);
        short value = BinaryPrimitives.ReadInt16BigEndian(_buffer.Slice(_position, 2));
        _position += 2;
        return value;
    }

    public int ReadI32()
    {
        EnsureAvailable(4);
        int value = BinaryPrimitives.ReadInt32BigEndian(_buffer.Slice(_position, 4));
        _position += 4;
        return value;
    }

    public ReadOnlySpan<byte> ReadRemaining()
    {
        ReadOnlySpan<byte> rest = _buffer.Slice(_position);
        _position = _buffer.Length;
        return rest;
    }

}