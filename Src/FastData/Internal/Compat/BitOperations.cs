#if !NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace System.Numerics;

internal static class BitOperations
{
    private static ReadOnlySpan<byte> TrailingZeroCountDeBruijn =>
    [
        00, 01, 28, 02, 29, 14, 24, 03,
        30, 22, 20, 15, 25, 17, 04, 08,
        31, 27, 13, 23, 21, 19, 16, 07,
        26, 12, 18, 06, 11, 05, 10, 09
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RotateLeft(uint value, int offset) => (value << offset) | (value >> (32 - offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateRight(uint value, int offset) => (value >> offset) | (value << (32 - offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateRight(ulong value, int offset) => (value >> offset) | (value << (64 - offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int PopCount(ulong value)
    {
        value -= (value >> 1) & 0x_55555555_55555555ul;
        value = (value & 0x_33333333_33333333ul) + ((value >> 2) & 0x_33333333_33333333ul);
        value = (((value + (value >> 4)) & 0x_0F0F0F0F_0F0F0F0Ful) * 0x_01010101_01010101ul) >> 56;

        return (int)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int LeadingZeroCount(ulong value)
    {
        if (value == 0)
            return 64;

        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value |= value >> 32;

        // Count the ones
        value -= (value >> 1) & 0x5555555555555555;
        value = ((value >> 2) & 0x3333333333333333) + (value & 0x3333333333333333);
        value = ((value >> 4) + value) & 0x0f0f0f0f0f0f0f0f;
        value += value >> 8;
        value += value >> 16;
        value += value >> 32;

        return 64 - (int)(value & 0x0000007f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int TrailingZeroCount(ulong value)
    {
        uint lo = (uint)value;

        if (lo == 0)
            return 32 + TrailingZeroCount((uint)(value >> 32));

        return TrailingZeroCount(lo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TrailingZeroCount(uint value)
    {
        if (value == 0)
            return 32;

        return Unsafe.AddByteOffset(
            ref MemoryMarshal.GetReference(TrailingZeroCountDeBruijn),
            (IntPtr)(int)(((value & (uint)-(int)value) * 0x077CB531u) >> 27));
    }
}
#endif