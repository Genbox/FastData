using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Numerics.BitOperations;

namespace Genbox.FastData.Internal.Encodings;

internal static class IntegerEncodingHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Get7BitEncodedLength(ulong value) => value == 0 ? 1 : ((64 - LeadingZeroCount(value)) + 6) / 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt64BE(ulong value, int length, Span<byte> destination)
    {
        unchecked
        {
            switch (length)
            {
                case 1:
                    destination[0] = (byte)value;
                    return;
                case 2:
                    WriteUInt16BigEndian(destination, (ushort)value);
                    return;
                case 3:
                    destination[0] = (byte)(value >> 16);
                    WriteUInt16BigEndian(destination.Slice(1), (ushort)value);
                    return;
                case 4:
                    BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)value);
                    return;
                case 5:
                    destination[0] = (byte)(value >> 32);
                    BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(1), (uint)value);
                    return;
                case 6:
                    WriteUInt16BigEndian(destination, (ushort)(value >> 32));
                    BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(2), (uint)value);
                    return;
                case 7:
                    destination[0] = (byte)(value >> 48);
                    WriteUInt16BigEndian(destination.Slice(1), (ushort)(value >> 32));
                    BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(3), (uint)value);
                    return;
                default:
                    BinaryPrimitives.WriteUInt64BigEndian(destination, value);
                    return;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReadUInt64BE(ReadOnlySpan<byte> source, int length) => length switch
    {
        1 => source[0],
        2 => ReadUInt16BigEndian(source),
        3 => ((ulong)source[0] << 16) | ReadUInt16BigEndian(source.Slice(1)),
        4 => BinaryPrimitives.ReadUInt32BigEndian(source),
        5 => ((ulong)source[0] << 32) | BinaryPrimitives.ReadUInt32BigEndian(source.Slice(1)),
        6 => ((ulong)ReadUInt16BigEndian(source) << 32) | BinaryPrimitives.ReadUInt32BigEndian(source.Slice(2)),
        7 => ((ulong)source[0] << 48) | ((ulong)ReadUInt16BigEndian(source.Slice(1)) << 32) | BinaryPrimitives.ReadUInt32BigEndian(source.Slice(3)),
        _ => BinaryPrimitives.ReadUInt64BigEndian(source)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt32BE(uint value, int length, Span<byte> destination)
    {
        unchecked
        {
            switch (length)
            {
                case 1:
                    destination[0] = (byte)value;
                    return;
                case 2:
                    WriteUInt16BigEndian(destination, (ushort)value);
                    return;
                case 3:
                    destination[0] = (byte)(value >> 16);
                    WriteUInt16BigEndian(destination.Slice(1), (ushort)value);
                    return;
                default:
                    BinaryPrimitives.WriteUInt32BigEndian(destination, value);
                    return;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReadUInt32BE(ReadOnlySpan<byte> source, int length) => length switch
    {
        1 => source[0],
        2 => ReadUInt16BigEndian(source),
        3 => ((uint)source[0] << 16) | ReadUInt16BigEndian(source.Slice(1)),
        _ => ReadUInt32BigEndian(source)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt64LE(ulong value, int length, Span<byte> destination)
    {
        unchecked
        {
            switch (length)
            {
                case 4:
                    WriteUInt32LittleEndian(destination, (uint)value);
                    return;
                case 5:
                    WriteUInt32LittleEndian(destination, (uint)value);
                    destination[4] = (byte)(value >> 32);
                    return;
                case 6:
                    WriteUInt32LittleEndian(destination, (uint)value);
                    destination[4] = (byte)(value >> 32);
                    destination[5] = (byte)(value >> 40);
                    return;
                case 7:
                    WriteUInt32LittleEndian(destination, (uint)value);
                    destination[4] = (byte)(value >> 32);
                    destination[5] = (byte)(value >> 40);
                    destination[6] = (byte)(value >> 48);
                    return;
                default:
                    WriteUInt64LittleEndian(destination, value);
                    return;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReadUInt64LE(ReadOnlySpan<byte> source, int length) => length switch
    {
        4 => ReadUInt32LittleEndian(source),
        5 => ReadUInt32LittleEndian(source) | ((ulong)source[4] << 32),
        6 => ReadUInt32LittleEndian(source) | ((ulong)source[4] << 32) | ((ulong)source[5] << 40),
        7 => ReadUInt32LittleEndian(source) | ((ulong)source[4] << 32) | ((ulong)source[5] << 40) | ((ulong)source[6] << 48),
        _ => ReadUInt64LittleEndian(source)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReadUInt32LE(ReadOnlySpan<byte> source, int length) => length switch
    {
        1 => source[0],
        2 => (uint)source[0] | ((uint)source[1] << 8),
        3 => (uint)source[0] | ((uint)source[1] << 8) | ((uint)source[2] << 16),
        _ => ReadUInt32LittleEndian(source)
    };
}