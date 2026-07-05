using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// QUIC variable-length integer encoding for 1, 2, 4, or 8 byte unsigned integers up to 2^62-1.
/// Reference: RFC 9000 section 16, Variable-Length Integer Encoding, https://www.rfc-editor.org/rfc/rfc9000.html#section-16
/// </summary>
internal sealed class QuicEncoding : IIntegerEncoding
{
    internal const ulong MaxValue = 0x3fffffffffffffffUL;
    internal static QuicEncoding Instance { get; } = new QuicEncoding();

    public int MaxEncodedLength => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value)
    {
        if (value > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), value, "QUIC varints support values up to 2^62-1.");

        return value switch
        {
            < 0x40 => 1,
            < 0x4000 => 2,
            < 0x40000000 => 4,
            _ => 8
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        if (value > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), value, "QUIC varints support values up to 2^62-1.");

        unchecked
        {
            if (value < 0x40)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value < 0x4000)
            {
                WriteUInt16BigEndian(destination, (ushort)((0x40 << 8) | value));
                return 2;
            }

            if (value < 0x40000000)
            {
                WriteUInt32BigEndian(destination, (uint)((0x80UL << 24) | value));
                return 4;
            }

            WriteUInt64BigEndian(destination, (0xc0UL << 56) | value);
            return 8;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.IsEmpty)
            return false;

        byte first = source[0];
        if (first < 0x40)
        {
            value = first;
            bytesRead = 1;
            return true;
        }

        if (first < 0x80)
        {
            if (source.Length < sizeof(ushort))
                return false;

            value = ((ulong)(first & 0x3f) << 8) | source[1];
            bytesRead = sizeof(ushort);
            return true;
        }

        if (first < 0xc0)
        {
            if (source.Length < sizeof(uint))
                return false;

            value = ReadUInt32BigEndian(source) & 0x3fffffffUL;
            bytesRead = sizeof(uint);
            return true;
        }

        if (source.Length < sizeof(ulong))
            return false;

        value = ReadUInt64BigEndian(source) & MaxValue;
        bytesRead = sizeof(ulong);
        return true;
    }
}