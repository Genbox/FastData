using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Misc;

internal static class SplitMix64
{
    private const ulong Increment = 0x9E3779B97F4A7C15;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Next(ulong state) => Next(ref state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Next(ref ulong state)
    {
        unchecked
        {
            state += Increment;
            ulong value = state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EB;
            return value ^ (value >> 31);
        }
    }
}