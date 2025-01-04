using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Compat;

internal static class BitOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RotateLeft(uint value, int offset) => (value << offset) | (value >> (32 - offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));
}