using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// Big-endian 7-bit variable-length quantity with an MSB continuation bit.
/// This is the generic VLQ layout used by MIDI variable-length quantities, extended here to the full UInt64 range.
/// References: MIDI 1.0 variable length quantity and general VLQ descriptions.
/// </summary>
internal sealed class VlqEncoding : IIntegerEncoding
{
    internal static VlqEncoding Instance { get; } = new VlqEncoding();

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
                destination[0] = (byte)((value >> 7) | 0x80);
                destination[1] = (byte)(value & 0x7f);
                return 2;
            }

            if (value < 1UL << 21)
            {
                destination[0] = (byte)((value >> 14) | 0x80);
                destination[1] = (byte)(((value >> 7) & 0x7f) | 0x80);
                destination[2] = (byte)(value & 0x7f);
                return 3;
            }

            if (value < 1UL << 28)
            {
                destination[0] = (byte)((value >> 21) | 0x80);
                destination[1] = (byte)(((value >> 14) & 0x7f) | 0x80);
                destination[2] = (byte)(((value >> 7) & 0x7f) | 0x80);
                destination[3] = (byte)(value & 0x7f);
                return 4;
            }

            if (value < 1UL << 35)
            {
                destination[0] = (byte)((value >> 28) | 0x80);
                destination[1] = (byte)(((value >> 21) & 0x7f) | 0x80);
                destination[2] = (byte)(((value >> 14) & 0x7f) | 0x80);
                destination[3] = (byte)(((value >> 7) & 0x7f) | 0x80);
                destination[4] = (byte)(value & 0x7f);
                return 5;
            }

            if (value < 1UL << 42)
            {
                destination[0] = (byte)((value >> 35) | 0x80);
                destination[1] = (byte)(((value >> 28) & 0x7f) | 0x80);
                destination[2] = (byte)(((value >> 21) & 0x7f) | 0x80);
                destination[3] = (byte)(((value >> 14) & 0x7f) | 0x80);
                destination[4] = (byte)(((value >> 7) & 0x7f) | 0x80);
                destination[5] = (byte)(value & 0x7f);
                return 6;
            }

            if (value < 1UL << 49)
            {
                destination[0] = (byte)((value >> 42) | 0x80);
                destination[1] = (byte)(((value >> 35) & 0x7f) | 0x80);
                destination[2] = (byte)(((value >> 28) & 0x7f) | 0x80);
                destination[3] = (byte)(((value >> 21) & 0x7f) | 0x80);
                destination[4] = (byte)(((value >> 14) & 0x7f) | 0x80);
                destination[5] = (byte)(((value >> 7) & 0x7f) | 0x80);
                destination[6] = (byte)(value & 0x7f);
                return 7;
            }

            if (value < 1UL << 56)
            {
                destination[0] = (byte)((value >> 49) | 0x80);
                destination[1] = (byte)(((value >> 42) & 0x7f) | 0x80);
                destination[2] = (byte)(((value >> 35) & 0x7f) | 0x80);
                destination[3] = (byte)(((value >> 28) & 0x7f) | 0x80);
                destination[4] = (byte)(((value >> 21) & 0x7f) | 0x80);
                destination[5] = (byte)(((value >> 14) & 0x7f) | 0x80);
                destination[6] = (byte)(((value >> 7) & 0x7f) | 0x80);
                destination[7] = (byte)(value & 0x7f);
                return 8;
            }

            if (value < 1UL << 63)
            {
                destination[0] = (byte)((value >> 56) | 0x80);
                destination[1] = (byte)(((value >> 49) & 0x7f) | 0x80);
                destination[2] = (byte)(((value >> 42) & 0x7f) | 0x80);
                destination[3] = (byte)(((value >> 35) & 0x7f) | 0x80);
                destination[4] = (byte)(((value >> 28) & 0x7f) | 0x80);
                destination[5] = (byte)(((value >> 21) & 0x7f) | 0x80);
                destination[6] = (byte)(((value >> 14) & 0x7f) | 0x80);
                destination[7] = (byte)(((value >> 7) & 0x7f) | 0x80);
                destination[8] = (byte)(value & 0x7f);
                return 9;
            }

            destination[0] = (byte)((value >> 63) | 0x80);
            destination[1] = (byte)(((value >> 56) & 0x7f) | 0x80);
            destination[2] = (byte)(((value >> 49) & 0x7f) | 0x80);
            destination[3] = (byte)(((value >> 42) & 0x7f) | 0x80);
            destination[4] = (byte)(((value >> 35) & 0x7f) | 0x80);
            destination[5] = (byte)(((value >> 28) & 0x7f) | 0x80);
            destination[6] = (byte)(((value >> 21) & 0x7f) | 0x80);
            destination[7] = (byte)(((value >> 14) & 0x7f) | 0x80);
            destination[8] = (byte)(((value >> 7) & 0x7f) | 0x80);
            destination[9] = (byte)(value & 0x7f);
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
        if (source.Length < 2) return false;

        b = source[1];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 2, out value, out bytesRead);
        if (source.Length < 3) return false;

        b = source[2];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 3, out value, out bytesRead);
        if (source.Length < 4) return false;

        b = source[3];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 4, out value, out bytesRead);
        if (source.Length < 5) return false;

        b = source[4];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 5, out value, out bytesRead);
        if (source.Length < 6) return false;

        b = source[5];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 6, out value, out bytesRead);
        if (source.Length < 7) return false;

        b = source[6];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 7, out value, out bytesRead);
        if (source.Length < 8) return false;

        b = source[7];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 8, out value, out bytesRead);
        if (source.Length < 9) return false;

        b = source[8];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 9, out value, out bytesRead);
        if (source.Length < 10 || result > ulong.MaxValue >> 7)
            return false;

        b = source[9];
        result = (result << 7) | (b & 0x7fUL);
        if (b >= 0x80)
            return false;

        value = result;
        bytesRead = 10;
        return true;
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
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 2, out value, out bytesRead);

        b = source[2];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 3, out value, out bytesRead);

        b = source[3];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 4, out value, out bytesRead);

        b = source[4];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 5, out value, out bytesRead);

        b = source[5];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 6, out value, out bytesRead);

        b = source[6];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 7, out value, out bytesRead);

        b = source[7];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 8, out value, out bytesRead);

        b = source[8];
        result = (result << 7) | (b & 0x7fUL);
        if (b < 0x80) return Success(result, 9, out value, out bytesRead);

        if (result > ulong.MaxValue >> 7)
        {
            value = 0;
            bytesRead = 0;
            return false;
        }

        b = source[9];
        if (b >= 0x80)
        {
            value = 0;
            bytesRead = 0;
            return false;
        }

        value = (result << 7) | b;
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