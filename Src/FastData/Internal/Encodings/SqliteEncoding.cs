using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// SQLite3 varint: a 1-9 byte big-endian static Huffman encoding of 64-bit two's-complement integers.
/// The first eight bytes carry 7 payload bits plus continuation; the ninth byte carries 8 payload bits.
/// Reference: SQLite database file format, variable-length integer section, https://sqlite.org/fileformat2.html#varint_format
/// </summary>
internal sealed class SqliteEncoding : IIntegerEncoding
{
    private const uint Slot20 = 0x001fc07f;
    private const uint Slot420 = 0xf01fc07f;

    internal static SqliteEncoding Instance { get; } = new SqliteEncoding();

    public int MaxEncodedLength => 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value) => value switch
    {
        <= 0x7f => 1,
        <= 0x3fff => 2,
        <= 0x1fffff => 3,
        <= 0x0fffffff => 4,
        <= 0x07ffffffff => 5,
        <= 0x03ffffffffff => 6,
        <= 0x01ffffffffffff => 7,
        <= 0x00ffffffffffffff => 8,
        _ => 9
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        unchecked
        {
            if (value <= 0x7f)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value <= 0x3fff)
            {
                destination[0] = (byte)(0x80 | (value >> 7));
                destination[1] = (byte)(value & 0x7f);
                return 2;
            }

            if (value <= 0x1fffff)
            {
                destination[0] = (byte)(0x80 | (value >> 14));
                destination[1] = (byte)(0x80 | ((value >> 7) & 0x7f));
                destination[2] = (byte)(value & 0x7f);
                return 3;
            }

            if (value <= 0x0fffffff)
            {
                destination[0] = (byte)(0x80 | (value >> 21));
                destination[1] = (byte)(0x80 | ((value >> 14) & 0x7f));
                destination[2] = (byte)(0x80 | ((value >> 7) & 0x7f));
                destination[3] = (byte)(value & 0x7f);
                return 4;
            }

            if (value <= 0x07ffffffff)
            {
                destination[0] = (byte)(0x80 | (value >> 28));
                destination[1] = (byte)(0x80 | ((value >> 21) & 0x7f));
                destination[2] = (byte)(0x80 | ((value >> 14) & 0x7f));
                destination[3] = (byte)(0x80 | ((value >> 7) & 0x7f));
                destination[4] = (byte)(value & 0x7f);
                return 5;
            }

            if (value <= 0x03ffffffffff)
            {
                destination[0] = (byte)(0x80 | (value >> 35));
                destination[1] = (byte)(0x80 | ((value >> 28) & 0x7f));
                destination[2] = (byte)(0x80 | ((value >> 21) & 0x7f));
                destination[3] = (byte)(0x80 | ((value >> 14) & 0x7f));
                destination[4] = (byte)(0x80 | ((value >> 7) & 0x7f));
                destination[5] = (byte)(value & 0x7f);
                return 6;
            }

            if (value <= 0x01ffffffffffff)
            {
                destination[0] = (byte)(0x80 | (value >> 42));
                destination[1] = (byte)(0x80 | ((value >> 35) & 0x7f));
                destination[2] = (byte)(0x80 | ((value >> 28) & 0x7f));
                destination[3] = (byte)(0x80 | ((value >> 21) & 0x7f));
                destination[4] = (byte)(0x80 | ((value >> 14) & 0x7f));
                destination[5] = (byte)(0x80 | ((value >> 7) & 0x7f));
                destination[6] = (byte)(value & 0x7f);
                return 7;
            }

            if (value <= 0x00ffffffffffffff)
            {
                destination[0] = (byte)(0x80 | (value >> 49));
                destination[1] = (byte)(0x80 | ((value >> 42) & 0x7f));
                destination[2] = (byte)(0x80 | ((value >> 35) & 0x7f));
                destination[3] = (byte)(0x80 | ((value >> 28) & 0x7f));
                destination[4] = (byte)(0x80 | ((value >> 21) & 0x7f));
                destination[5] = (byte)(0x80 | ((value >> 14) & 0x7f));
                destination[6] = (byte)(0x80 | ((value >> 7) & 0x7f));
                destination[7] = (byte)(value & 0x7f);
                return 8;
            }

            destination[0] = (byte)(0x80 | (value >> 57));
            destination[1] = (byte)(0x80 | ((value >> 50) & 0x7f));
            destination[2] = (byte)(0x80 | ((value >> 43) & 0x7f));
            destination[3] = (byte)(0x80 | ((value >> 36) & 0x7f));
            destination[4] = (byte)(0x80 | ((value >> 29) & 0x7f));
            destination[5] = (byte)(0x80 | ((value >> 22) & 0x7f));
            destination[6] = (byte)(0x80 | ((value >> 15) & 0x7f));
            destination[7] = (byte)(0x80 | ((value >> 8) & 0x7f));
            destination[8] = (byte)value;
            return 9;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.IsEmpty)
            return false;

        if (source[0] < 0x80)
        {
            value = source[0];
            bytesRead = 1;
            return true;
        }

        if (source.Length < 2)
            return Fail(out value, out bytesRead);

        if (source[1] < 0x80)
        {
            value = ((uint)(source[0] & 0x7f) << 7) | source[1];
            bytesRead = 2;
            return true;
        }

        if (source.Length < 3)
            return Fail(out value, out bytesRead);

        uint a = (uint)source[0] << 14;
        uint b = source[1];
        a |= source[2];
        if ((a & 0x80) == 0)
        {
            a &= Slot20;
            b = (b & 0x7f) << 7;
            value = a | b;
            bytesRead = 3;
            return true;
        }

        if (source.Length < 4)
            return Fail(out value, out bytesRead);

        a &= Slot20;
        b = (b << 14) | source[3];
        if ((b & 0x80) == 0)
        {
            b &= Slot20;
            a = (a << 7) | b;
            value = a;
            bytesRead = 4;
            return true;
        }

        b &= Slot20;
        uint s = a;

        if (source.Length < 5)
            return Fail(out value, out bytesRead);

        a = (a << 14) | source[4];
        if ((a & 0x80) == 0)
        {
            b <<= 7;
            a |= b;
            s >>= 18;
            value = ((ulong)s << 32) | a;
            bytesRead = 5;
            return true;
        }

        s = (s << 7) | b;

        if (source.Length < 6)
            return Fail(out value, out bytesRead);

        b = (b << 14) | source[5];
        if ((b & 0x80) == 0)
        {
            a &= Slot20;
            a = (a << 7) | b;
            s >>= 18;
            value = ((ulong)s << 32) | a;
            bytesRead = 6;
            return true;
        }

        if (source.Length < 7)
            return Fail(out value, out bytesRead);

        a = (a << 14) | source[6];
        if ((a & 0x80) == 0)
        {
            a &= Slot420;
            b &= Slot20;
            b <<= 7;
            a |= b;
            s >>= 11;
            value = ((ulong)s << 32) | a;
            bytesRead = 7;
            return true;
        }

        a &= Slot20;

        if (source.Length < 8)
            return Fail(out value, out bytesRead);

        b = (b << 14) | source[7];
        if ((b & 0x80) == 0)
        {
            b &= Slot420;
            a = (a << 7) | b;
            s >>= 4;
            value = ((ulong)s << 32) | a;
            bytesRead = 8;
            return true;
        }

        if (source.Length < 9)
            return Fail(out value, out bytesRead);

        a = (a << 15) | source[8];
        b &= Slot20;
        b <<= 8;
        a |= b;

        s <<= 4;
        b = source[4] & 0x7fU;
        b >>= 3;
        s |= b;

        value = ((ulong)s << 32) | a;
        bytesRead = 9;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Fail(out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;
        return false;
    }
}