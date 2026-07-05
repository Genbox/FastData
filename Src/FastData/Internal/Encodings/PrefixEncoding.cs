using System.Numerics;
using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// PrefixVarInt unsigned integer encoding. The first byte stores both a unary byte-length prefix and the low value bits, followed by little-endian payload bytes.
/// Reference: Chromium PrefixVarInt, https://chromium.googlesource.com/chromiumos/third_party/libtextclassifier/+/adbbad2e0138453af45cc08cb3d04317ae2b8ba1/utils/base/prefixvarint.h
/// </summary>
internal sealed class PrefixEncoding : IIntegerEncoding
{
    internal static PrefixEncoding Instance { get; } = new PrefixEncoding();

    public int MaxEncodedLength => 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value)
    {
        if (value < 1UL << 7)
            return 1;

        int bitLength = 64 - BitOperations.LeadingZeroCount(value);
        return Math.Min(9, (bitLength + 6) / 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        unchecked
        {
            if (value < 1UL << 7)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value < 1UL << 14)
            {
                value <<= 2;
                destination[0] = (byte)(0x80 | ((byte)value >> 2));
                destination[1] = (byte)(value >> 8);
                return 2;
            }

            if (value < 1UL << 21)
            {
                value <<= 3;
                destination[0] = (byte)(0xc0 | ((byte)value >> 3));
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)(value >> 16);
                return 3;
            }

            if (value < 1UL << 28)
            {
                value <<= 4;
                destination[0] = (byte)(0xe0 | ((byte)value >> 4));
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)(value >> 16);
                destination[3] = (byte)(value >> 24);
                return 4;
            }

            if (value < 1UL << 35)
            {
                value <<= 5;
                destination[0] = (byte)(0xf0 | ((byte)value >> 5));
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)(value >> 16);
                destination[3] = (byte)(value >> 24);
                destination[4] = (byte)(value >> 32);
                return 5;
            }

            if (value < 1UL << 42)
            {
                value <<= 6;
                destination[0] = (byte)(0xf8 | ((byte)value >> 6));
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)(value >> 16);
                destination[3] = (byte)(value >> 24);
                destination[4] = (byte)(value >> 32);
                destination[5] = (byte)(value >> 40);
                return 6;
            }

            if (value < 1UL << 49)
            {
                value <<= 7;
                destination[0] = (byte)(0xfc | ((byte)value >> 7));
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)(value >> 16);
                destination[3] = (byte)(value >> 24);
                destination[4] = (byte)(value >> 32);
                destination[5] = (byte)(value >> 40);
                destination[6] = (byte)(value >> 48);
                return 7;
            }

            if (value < 1UL << 56)
            {
                destination[0] = 0xfe;
                IntegerEncodingHelpers.WriteUInt64LE(value, 7, destination.Slice(1));
                return 8;
            }

            destination[0] = 0xff;
            WriteUInt64LittleEndian(destination.Slice(1), value);
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

        byte first = source[0];
        if (first < 0x80)
        {
            value = first;
            bytesRead = 1;
            return true;
        }

        int length = GetLengthFromFirstByte(first);
        if (source.Length < length)
            return false;

        value = length switch
        {
            2 => (first & 0x3fUL) | ((ulong)source[1] << 6),
            3 => (first & 0x1fUL) | ((ulong)source[1] << 5) | ((ulong)source[2] << 13),
            4 => (first & 0x0fUL) | ((ulong)source[1] << 4) | ((ulong)source[2] << 12) | ((ulong)source[3] << 20),
            5 => (first & 0x07UL) | ((ulong)source[1] << 3) | ((ulong)source[2] << 11) | ((ulong)source[3] << 19) | ((ulong)source[4] << 27),
            6 => (first & 0x03UL) | ((ulong)source[1] << 2) | ((ulong)source[2] << 10) | ((ulong)source[3] << 18) | ((ulong)source[4] << 26) | ((ulong)source[5] << 34),
            7 => (first & 0x01UL) | ((ulong)source[1] << 1) | ((ulong)source[2] << 9) | ((ulong)source[3] << 17) | ((ulong)source[4] << 25) | ((ulong)source[5] << 33) | ((ulong)source[6] << 41),
            8 => IntegerEncodingHelpers.ReadUInt64LE(source.Slice(1), 7),
            _ => ReadUInt64LittleEndian(source.Slice(1))
        };

        bytesRead = length;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetLengthFromFirstByte(byte first) => first switch
    {
        < 0xc0 => 2,
        < 0xe0 => 3,
        < 0xf0 => 4,
        < 0xf8 => 5,
        < 0xfc => 6,
        < 0xfe => 7,
        _ => first == 0xfe ? 8 : 9
    };
}