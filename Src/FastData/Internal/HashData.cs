using System.Numerics;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal;

/// <summary>Used internally in FastData to store hash codes and their properties.</summary>
internal record HashData(ulong[] HashCodes, float CapacityFactor, int TableSize, bool OptimizeHashTableBucketSize, bool RoundModuloToPowerOfTwo, float RoundModuloToPowerOfTwoThreshold, bool HashCodesPerfect, int CollisionCount, ulong MinHashCode, ulong MaxHashCode)
{
    internal static HashData Create<T>(ReadOnlySpan<T> data, float capacityFactor, NumericHashFunc<T> func) => Create(data, capacityFactor, false, false, 0, func);

    internal static HashData Create<T>(ReadOnlySpan<T> data, float capacityFactor, bool roundModuloToPowerOfTwo, float roundModuloToPowerOfTwoThreshold, NumericHashFunc<T> func) => Create(data, capacityFactor, false, roundModuloToPowerOfTwo, roundModuloToPowerOfTwoThreshold, func);

    internal static HashData Create<T>(ReadOnlySpan<T> data, float capacityFactor, bool optimizeHashTableBucketSize, bool roundModuloToPowerOfTwo, float roundModuloToPowerOfTwoThreshold, NumericHashFunc<T> func)
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

        int tableSize = optimizeHashTableBucketSize ? GetOptimizedBucketTableSize(baseTableSize, hashCodes, out int collisions) : baseTableSize;
        tableSize = GetModuloLength(tableSize, roundModuloToPowerOfTwo, roundModuloToPowerOfTwoThreshold, hashCodes, out collisions);
        bool perfect = collisions == 0;
        return new HashData(hashCodes, capacityFactor, tableSize, optimizeHashTableBucketSize, roundModuloToPowerOfTwo, roundModuloToPowerOfTwoThreshold, perfect, collisions, minHashCode, maxHashCode);
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

    private static int GetOptimizedBucketTableSize(int baseLength, ReadOnlySpan<ulong> hashCodes, out int collisions)
    {
        const double AcceptableCollisionRate = 0.05;
        const int LargeInputSizeThreshold = 1000;
        const int MaxSmallBucketTableMultiplier = 16;
        const int MaxLargeBucketTableMultiplier = 3;
        const int MaxCandidateCount = 256;

        collisions = CountBucketCollisions(hashCodes, baseLength);

        if (collisions == 0)
            return baseLength;

        if (baseLength == int.MaxValue)
            return baseLength;

        int multiplier = hashCodes.Length >= LargeInputSizeThreshold ? MaxLargeBucketTableMultiplier : MaxSmallBucketTableMultiplier;
        long maxByMultiplier = (long)hashCodes.Length * multiplier;
        long maxByCandidates = (long)baseLength + MaxCandidateCount;
        int maxLength = (int)Math.Min(int.MaxValue, Math.Max(baseLength, Math.Min(maxByMultiplier, maxByCandidates)));
        int bestLength = baseLength;

        for (int candidate = baseLength + 1; candidate <= maxLength; candidate++)
        {
            int candidateCollisions = CountBucketCollisions(hashCodes, candidate);

            if (candidateCollisions >= collisions)
                continue;

            bestLength = candidate;
            collisions = candidateCollisions;

            if (candidateCollisions / (double)hashCodes.Length <= AcceptableCollisionRate)
                break;
        }

        return bestLength;
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