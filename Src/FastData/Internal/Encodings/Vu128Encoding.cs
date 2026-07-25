using System.Numerics;
using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Runtime.CompilerServices.Unsafe;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// vu128 efficient variable-length integer encoding. Vu128 supports up to u128 bit, but this implementation is limited to u64 values.
/// Values in the range [0, 2^7) are encoded as a single byte with the same bits as the original value. The MSB is zero.
/// Values in the range [2^7, 2^28) are encoded as a unary length prefix, followed by (length*7) bits in little endian order.
/// Values in the range [2^28, 2^64) are encoded as a binary length prefix, followed by payload bytes, in little-endian order.
/// Reference: John Millikin, vu128: Efficient variable-length integers, https://john-millikin.com/vu128-efficient-variable-length-integers
/// Source: https://github.com/jmillikin/rust-vu128
/// </summary>
internal sealed class Vu128Encoding : IIntegerEncoding
{
    internal const int MaxUInt32EncodedLength = 5;
    internal static Vu128Encoding Instance { get; } = new Vu128Encoding();

    public int MaxEncodedLength => 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value)
    {
        if (value <= uint.MaxValue)
            return GetEncodedLength((uint)value);

        const int LenMask = 0b111;
        int len = (BitOperations.LeadingZeroCount(value) >> 3) ^ LenMask;
        return len + 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        unchecked
        {
            if (value < 0x80)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value < 0x10000000)
            {
                if (value < 0x00004000)
                {
                    value <<= 2;
                    destination[0] = (byte)(0x80 | ((byte)value >> 2));
                    destination[1] = (byte)(value >> 8);
                    return 2;
                }

                if (value < 0x00200000)
                {
                    value <<= 3;
                    destination[0] = (byte)(0xc0 | ((byte)value >> 3));
                    destination[1] = (byte)(value >> 8);
                    destination[2] = (byte)(value >> 16);
                    return 3;
                }

                value <<= 4;
                destination[0] = (byte)(0xe0 | ((byte)value >> 4));
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)(value >> 16);
                destination[3] = (byte)(value >> 24);
                return 4;
            }

            int len = (BitOperations.LeadingZeroCount(value) >> 3) ^ 0b111;
            int payloadLength = len + 1;
            IntegerEncodingHelpers.WriteUInt64LE(value, payloadLength, destination.Slice(1));

            destination[0] = (byte)(0xf0 | len);
            return len + 2;
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
        if ((first & 0x80) == 0)
        {
            value = first;
            bytesRead = 1;
            return true;
        }

        if (first < 0xf0)
        {
            if ((first & 0x40) == 0)
            {
                if (source.Length < 2)
                    return false;

                value = ((ulong)source[1] << 6) | (first & 0x3fUL);
                bytesRead = 2;
                return true;
            }

            if ((first & 0x20) == 0)
            {
                if (source.Length < 3)
                    return false;

                value = ((ulong)source[2] << 13) | ((ulong)source[1] << 5) | (first & 0x1fUL);
                bytesRead = 3;
                return true;
            }

            if (source.Length < 4)
                return false;

            value = ((ulong)source[3] << 20) | ((ulong)source[2] << 12) | ((ulong)source[1] << 4) | (first & 0x0fUL);
            bytesRead = 4;
            return true;
        }

        int len = first & 0x0f;
        if (len > 7)
            return false;

        int length = len + 2;
        if (source.Length < length)
            return false;

        value = IntegerEncodingHelpers.ReadUInt64LE(source.Slice(1), len + 1);

        bytesRead = length;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(uint value)
    {
        switch (value)
        {
            case < 0x80:
                return 1;
            case < 0x4000:
                return 2;
            case < 0x200000:
                return 3;
            case < 0x10000000:
                return 4;
        }

        return MaxUInt32EncodedLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(float value) => GetEncodedLength(ReverseEndianness(ReadUnaligned<uint>(ref As<float, byte>(ref value))));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(double value) => GetEncodedLength(ReverseEndianness(ReadUnaligned<ulong>(ref As<double, byte>(ref value))));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(uint value, Span<byte> destination)
    {
        unchecked
        {
            if (value < 0x80)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value < 0x10000000)
            {
                if (value < 0x00004000)
                {
                    value <<= 2;
                    destination[0] = (byte)(0x80 | ((byte)value >> 2));
                    destination[1] = (byte)(value >> 8);
                    return 2;
                }

                if (value < 0x00200000)
                {
                    value <<= 3;
                    destination[0] = (byte)(0xc0 | ((byte)value >> 3));
                    destination[1] = (byte)(value >> 8);
                    destination[2] = (byte)(value >> 16);
                    return 3;
                }

                value <<= 4;
                destination[0] = (byte)(0xe0 | ((byte)value >> 4));
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)(value >> 16);
                destination[3] = (byte)(value >> 24);
                return 4;
            }

            destination[0] = 0xf3;
            WriteUInt32LittleEndian(destination.Slice(1), value);
            return MaxUInt32EncodedLength;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(float value, Span<byte> destination) => Encode(ReverseEndianness(ReadUnaligned<uint>(ref As<float, byte>(ref value))), destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(double value, Span<byte> destination) => Encode(ReverseEndianness(ReadUnaligned<ulong>(ref As<double, byte>(ref value))), destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out uint value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.IsEmpty)
            return false;

        byte first = source[0];
        if ((first & 0x80) == 0)
        {
            value = first;
            bytesRead = 1;
            return true;
        }

        if (first < 0xf0)
        {
            if ((first & 0x40) == 0)
            {
                if (source.Length < 2)
                    return false;

                value = ((uint)source[1] << 6) | (first & 0x3fU);
                bytesRead = 2;
                return true;
            }

            if ((first & 0x20) == 0)
            {
                if (source.Length < 3)
                    return false;

                value = ((uint)source[2] << 13) | ((uint)source[1] << 5) | (first & 0x1fU);
                bytesRead = 3;
                return true;
            }

            if (source.Length < 4)
                return false;

            value = ((uint)source[3] << 20) | ((uint)source[2] << 12) | ((uint)source[1] << 4) | (first & 0x0fU);
            bytesRead = 4;
            return true;
        }

        int len = first & 0x0f;
        int length = len + 2;
        if (len > 3 || source.Length < length)
            return false;

        value = IntegerEncodingHelpers.ReadUInt32LE(source.Slice(1), len + 1);

        bytesRead = length;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out float value, out int bytesRead)
    {
        value = 0;
        if (!TryDecode(source, out uint bits, out bytesRead))
            return false;

        uint value1 = ReverseEndianness(bits);
        value = ReadUnaligned<float>(ref As<uint, byte>(ref value1));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out double value, out int bytesRead)
    {
        value = 0;
        if (!TryDecode(source, out ulong bits, out bytesRead))
            return false;

        ulong value1 = ReverseEndianness(bits);
        value = ReadUnaligned<double>(ref As<ulong, byte>(ref value1));
        return true;
    }
}