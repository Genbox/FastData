using System.Numerics;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal;

/// <summary>Used internally in FastData to store hash codes and their properties.</summary>
internal record HashData(ulong[] HashCodes, float CapacityFactor, int TableSize, bool RoundModuloToPowerOfTwo, float RoundModuloToPowerOfTwoThreshold, bool HashCodesPerfect, ulong MinHashCode, ulong MaxHashCode)
{
    internal static HashData Create<T>(ReadOnlySpan<T> data, float capacityFactor, NumericHashFunc<T> func) => Create(data, capacityFactor, false, 0, func);

    internal static HashData Create<T>(ReadOnlySpan<T> data, float capacityFactor, bool roundModuloToPowerOfTwo, float roundModuloToPowerOfTwoThreshold, NumericHashFunc<T> func)
    {
        int baseTableSize = GetBaseTableSize(data.Length, capacityFactor);
        ulong[] hashCodes = new ulong[data.Length];

        ulong minHashCode = ulong.MaxValue;
        ulong maxHashCode = ulong.MinValue;

        for (int i = 0; i < data.Length; i++)
        {
            ulong hash = func(data[i]);
            hashCodes[i] = hash;

            minHashCode = Math.Min(minHashCode, hash);
            maxHashCode = Math.Max(maxHashCode, hash);
        }

        int tableSize = GetModuloLength(baseTableSize, roundModuloToPowerOfTwo, roundModuloToPowerOfTwoThreshold, hashCodes, out int collisions);
        bool perfect = collisions == 0;
        return new HashData(hashCodes, capacityFactor, tableSize, roundModuloToPowerOfTwo, roundModuloToPowerOfTwoThreshold, perfect, minHashCode, maxHashCode);
    }

    /// <summary>Round <paramref name="length" /> to the next power of two if within threshold. Does not compare collisions because callers use this for non-bucket dimensions (e.g. bloom filter word count).</summary>
    internal int GetModuloLength(int length) => GetModuloLength(length, RoundModuloToPowerOfTwo, RoundModuloToPowerOfTwoThreshold);

    private static int GetBaseTableSize(int count, float capacityFactor)
    {
        if (float.IsNaN(capacityFactor) || float.IsInfinity(capacityFactor) || capacityFactor <= 0)
            throw new InvalidOperationException("HashTableCapacityFactor must be a finite value greater than 0.");

        double tableSize = Math.Ceiling(count * (double)capacityFactor);

        if (tableSize > int.MaxValue)
            throw new InvalidOperationException("HashTableCapacityFactor results in a hash table that is too large.");

        return Math.Max(1, (int)tableSize);
    }

    private static int GetModuloLength(int length, bool roundModuloToPowerOfTwo, float roundingThreshold)
    {
        if (length <= 0)
            throw new InvalidOperationException("Modulo length must be greater than zero.");

        uint current = (uint)length;

        if (!roundModuloToPowerOfTwo || BitOperations.IsPow2(current))
            return length;

        if (float.IsNaN(roundingThreshold) || float.IsInfinity(roundingThreshold) || roundingThreshold < 0)
            throw new InvalidOperationException("RoundModuloToPowerOfTwoThreshold must be a finite value greater than or equal to zero.");

        uint rounded = BitOperations.RoundUpToPowerOf2(current);

        if (rounded == 0 || rounded > int.MaxValue)
            return length;

        double overhead = (double)(rounded - current) / rounded;
        if (overhead > roundingThreshold)
            return length;

        return (int)rounded;
    }

    private static int GetModuloLength(int length, bool roundModuloToPowerOfTwo, float roundingThreshold, ReadOnlySpan<ulong> hashCodes, out int collisions)
    {
        if (length <= 0)
            throw new InvalidOperationException("Modulo length must be greater than zero.");

        uint current = (uint)length;

        if (!roundModuloToPowerOfTwo || BitOperations.IsPow2(current))
        {
            collisions = CountBucketCollisions(hashCodes, length);
            return length;
        }

        if (float.IsNaN(roundingThreshold) || float.IsInfinity(roundingThreshold) || roundingThreshold < 0)
            throw new InvalidOperationException("RoundModuloToPowerOfTwoThreshold must be a finite value greater than or equal to zero.");

        uint rounded = BitOperations.RoundUpToPowerOf2(current);

        if (rounded == 0 || rounded > int.MaxValue)
        {
            collisions = CountBucketCollisions(hashCodes, length);
            return length;
        }

        double overhead = (double)(rounded - current) / rounded;
        if (overhead > roundingThreshold)
        {
            collisions = CountBucketCollisions(hashCodes, length);
            return length;
        }

        int roundedLength = (int)rounded;

        int currentCollisions = CountBucketCollisions(hashCodes, length);
        int roundedCollisions = CountBucketCollisions(hashCodes, roundedLength);

        if (roundedCollisions > currentCollisions)
        {
            collisions = currentCollisions;
            return length;
        }

        collisions = roundedCollisions;
        return roundedLength;
    }

    private static int CountBucketCollisions(ReadOnlySpan<ulong> hashCodes, int length)
    {
        SwitchingBitSet tracker = new SwitchingBitSet(length, false);
        int collisions = 0;

        for (int i = 0; i < hashCodes.Length; i++)
        {
            if (!tracker.Add((uint)(hashCodes[i] % (uint)length)))
                collisions++;
        }

        return collisions;
    }
}