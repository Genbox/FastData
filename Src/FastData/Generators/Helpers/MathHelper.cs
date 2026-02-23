using System.Runtime.CompilerServices;

namespace Genbox.FastData.Generators.Helpers;

/// <summary>Provides mathematical helper methods and prime number utilities for code generation and hashing operations.</summary>
public static class MathHelper
{
    /// <summary>Calculates a fast modulus multiplier for the given divisor.</summary>
    /// <param name="divisor">The divisor to use for modulus operations.</param>
    /// <returns>The fast modulus multiplier.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetFastModMultiplier(uint divisor) => (ulong.MaxValue / divisor) + 1;

    /// <summary>Computes the modulus of a value using a precomputed multiplier for performance.</summary>
    /// <param name="value">The value to mod.</param>
    /// <param name="divisor">The divisor.</param>
    /// <param name="multiplier">The precomputed multiplier.</param>
    /// <returns>The result of value mod divisor.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FastMod(uint value, uint divisor, ulong multiplier) => unchecked(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

    /// <summary>Determines whether the specified value is a power of two.</summary>
    /// <param name="x">The value to check.</param>
    /// <returns><see langword="true" /> if the value is a power of two; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(ulong x) => x != 0 && (x & (x - 1)) == 0;
}