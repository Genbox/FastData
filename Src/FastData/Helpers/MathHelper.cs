using System.Runtime.CompilerServices;

namespace Genbox.FastData.Helpers;

public static class MathHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetFastModMultiplier(uint divisor) => (ulong.MaxValue / divisor) + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FastMod(uint value, uint divisor, ulong multiplier) => unchecked((uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(ulong x) => x != 0 && (x & (x - 1)) == 0;
}