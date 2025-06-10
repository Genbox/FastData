using System.Runtime.CompilerServices;

namespace Genbox.FastData.Generators.Helpers;

/// <summary>Provides mathematical helper methods and prime number utilities for code generation and hashing operations.</summary>
public static class MathHelper
{
    private const int HashPrime = 101;

    /// <summary>Gets an array of predefined prime numbers to use for hash table sizing.</summary>
    internal static uint[] Primes =>
    [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    ];

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

    /// <summary>Returns the smallest prime number that is greater than or equal to the specified minimum value.</summary>
    /// <param name="min">The minimum value.</param>
    /// <returns>The smallest prime number greater than or equal to <paramref name="min" />.</returns>
    public static uint GetPrime(uint min)
    {
        foreach (uint prime in Primes)
        {
            if (prime >= min)
                return prime;
        }

        // Outside our predefined table. Compute the hard way.
        for (uint i = min | 1; i < int.MaxValue; i += 2)
        {
            if (IsPrime(i) && (i - 1) % HashPrime != 0)
                return i;
        }
        return min;
    }

    /// <summary>Determines whether the specified candidate is a prime number.</summary>
    /// <param name="candidate">The number to check for primality.</param>
    /// <returns><see langword="true" /> if the candidate is prime; otherwise, <see langword="false" />.</returns>
    private static bool IsPrime(uint candidate)
    {
        if ((candidate & 1) != 0)
        {
            int limit = (int)Math.Sqrt(candidate);
            for (int divisor = 3; divisor <= limit; divisor += 2)
            {
                if (candidate % divisor == 0)
                    return false;
            }
            return true;
        }
        return candidate == 2;
    }
}