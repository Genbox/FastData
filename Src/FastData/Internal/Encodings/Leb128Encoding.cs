using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// Unsigned base-128 varint using little-endian 7-bit groups and an MSB continuation bit.
/// This is the generic wire format used by unsigned LEB128, Protocol Buffers varints, and many vbyte variants.
/// References: DWARF LEB128, WebAssembly binary format, and Protocol Buffers encoding guide at https://protobuf.dev/programming-guides/encoding/#varints.
/// </summary>
internal sealed class Leb128Encoding : IIntegerEncoding
{
    internal static Leb128Encoding Instance { get; } = new Leb128Encoding();

    public int MaxEncodedLength => 10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value) => IntegerEncodingHelpers.Get7BitEncodedLength(value);

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
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)(value >> 7);
                return 2;
            }

            if (value < 1UL << 21)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)(value >> 14);
                return 3;
            }

            if (value < 1UL << 28)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)((value >> 14) | 0x80);
                destination[3] = (byte)(value >> 21);
                return 4;
            }

            if (value < 1UL << 35)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)((value >> 14) | 0x80);
                destination[3] = (byte)((value >> 21) | 0x80);
                destination[4] = (byte)(value >> 28);
                return 5;
            }

            if (value < 1UL << 42)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)((value >> 14) | 0x80);
                destination[3] = (byte)((value >> 21) | 0x80);
                destination[4] = (byte)((value >> 28) | 0x80);
                destination[5] = (byte)(value >> 35);
                return 6;
            }

            if (value < 1UL << 49)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)((value >> 14) | 0x80);
                destination[3] = (byte)((value >> 21) | 0x80);
                destination[4] = (byte)((value >> 28) | 0x80);
                destination[5] = (byte)((value >> 35) | 0x80);
                destination[6] = (byte)(value >> 42);
                return 7;
            }

            if (value < 1UL << 56)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)((value >> 14) | 0x80);
                destination[3] = (byte)((value >> 21) | 0x80);
                destination[4] = (byte)((value >> 28) | 0x80);
                destination[5] = (byte)((value >> 35) | 0x80);
                destination[6] = (byte)((value >> 42) | 0x80);
                destination[7] = (byte)(value >> 49);
                return 8;
            }

            if (value < 1UL << 63)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)((value >> 14) | 0x80);
                destination[3] = (byte)((value >> 21) | 0x80);
                destination[4] = (byte)((value >> 28) | 0x80);
                destination[5] = (byte)((value >> 35) | 0x80);
                destination[6] = (byte)((value >> 42) | 0x80);
                destination[7] = (byte)((value >> 49) | 0x80);
                destination[8] = (byte)(value >> 56);
                return 9;
            }

            destination[0] = (byte)(value | 0x80);
            destination[1] = (byte)((value >> 7) | 0x80);
            destination[2] = (byte)((value >> 14) | 0x80);
            destination[3] = (byte)((value >> 21) | 0x80);
            destination[4] = (byte)((value >> 28) | 0x80);
            destination[5] = (byte)((value >> 35) | 0x80);
            destination[6] = (byte)((value >> 42) | 0x80);
            destination[7] = (byte)((value >> 49) | 0x80);
            destination[8] = (byte)((value >> 56) | 0x80);
            destination[9] = (byte)(value >> 63);
            return 10;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.Length < 1)
            return false;

        if (source.Length >= MaxEncodedLength)
            return TryDecodeFull(source, out value, out bytesRead);

        byte b = source[0];
        if (b < 0x80)
        {
            value = b;
            bytesRead = 1;
            return true;
        }

        ulong result = b & 0x7fUL;
        if (source.Length < 2)
            return false;

        b = source[1];
        result |= (ulong)(b & 0x7f) << 7;
        if (b < 0x80) return Success(result, 2, out value, out bytesRead);
        if (source.Length < 3) return false;

        b = source[2];
        result |= (ulong)(b & 0x7f) << 14;
        if (b < 0x80) return Success(result, 3, out value, out bytesRead);
        if (source.Length < 4) return false;

        b = source[3];
        result |= (ulong)(b & 0x7f) << 21;
        if (b < 0x80) return Success(result, 4, out value, out bytesRead);
        if (source.Length < 5) return false;

        b = source[4];
        result |= (ulong)(b & 0x7f) << 28;
        if (b < 0x80) return Success(result, 5, out value, out bytesRead);
        if (source.Length < 6) return false;

        b = source[5];
        result |= (ulong)(b & 0x7f) << 35;
        if (b < 0x80) return Success(result, 6, out value, out bytesRead);
        if (source.Length < 7) return false;

        b = source[6];
        result |= (ulong)(b & 0x7f) << 42;
        if (b < 0x80) return Success(result, 7, out value, out bytesRead);
        if (source.Length < 8) return false;

        b = source[7];
        result |= (ulong)(b & 0x7f) << 49;
        if (b < 0x80) return Success(result, 8, out value, out bytesRead);
        if (source.Length < 9) return false;

        b = source[8];
        result |= (ulong)(b & 0x7f) << 56;
        if (b < 0x80) return Success(result, 9, out value, out bytesRead);
        if (source.Length < 10) return false;

        b = source[9];
        if ((b & 0xfe) != 0)
            return false;

        value = result | ((ulong)b << 63);
        bytesRead = 10;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(uint value) => IntegerEncodingHelpers.Get7BitEncodedLength(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(uint value, Span<byte> destination)
    {
        unchecked
        {
            if (value < 1U << 7)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value < 1U << 14)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)(value >> 7);
                return 2;
            }

            if (value < 1U << 21)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)(value >> 14);
                return 3;
            }

            if (value < 1U << 28)
            {
                destination[0] = (byte)(value | 0x80);
                destination[1] = (byte)((value >> 7) | 0x80);
                destination[2] = (byte)((value >> 14) | 0x80);
                destination[3] = (byte)(value >> 21);
                return 4;
            }

            destination[0] = (byte)(value | 0x80);
            destination[1] = (byte)((value >> 7) | 0x80);
            destination[2] = (byte)((value >> 14) | 0x80);
            destination[3] = (byte)((value >> 21) | 0x80);
            destination[4] = (byte)(value >> 28);
            return 5;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryDecodeFull(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        byte b = source[0];
        if (b < 0x80)
        {
            value = b;
            bytesRead = 1;
            return true;
        }

        ulong result = b & 0x7fUL;

        b = source[1];
        result |= (ulong)(b & 0x7f) << 7;
        if (b < 0x80) return Success(result, 2, out value, out bytesRead);

        b = source[2];
        result |= (ulong)(b & 0x7f) << 14;
        if (b < 0x80) return Success(result, 3, out value, out bytesRead);

        b = source[3];
        result |= (ulong)(b & 0x7f) << 21;
        if (b < 0x80) return Success(result, 4, out value, out bytesRead);

        b = source[4];
        result |= (ulong)(b & 0x7f) << 28;
        if (b < 0x80) return Success(result, 5, out value, out bytesRead);

        b = source[5];
        result |= (ulong)(b & 0x7f) << 35;
        if (b < 0x80) return Success(result, 6, out value, out bytesRead);

        b = source[6];
        result |= (ulong)(b & 0x7f) << 42;
        if (b < 0x80) return Success(result, 7, out value, out bytesRead);

        b = source[7];
        result |= (ulong)(b & 0x7f) << 49;
        if (b < 0x80) return Success(result, 8, out value, out bytesRead);

        b = source[8];
        result |= (ulong)(b & 0x7f) << 56;
        if (b < 0x80) return Success(result, 9, out value, out bytesRead);

        b = source[9];
        if ((b & 0xfe) != 0)
        {
            value = 0;
            bytesRead = 0;
            return false;
        }

        value = result | ((ulong)b << 63);
        bytesRead = 10;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Success(ulong result, int length, out ulong value, out int bytesRead)
    {
        value = result;
        bytesRead = length;
        return true;
    }
}