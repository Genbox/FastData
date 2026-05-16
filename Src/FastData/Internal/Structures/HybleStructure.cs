using System.Buffers.Binary;
using System.Numerics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

/// <summary>
/// Hyble is a displacement-based perfect hash structure.
/// Construction retries with different seeds. Each attempt multiplies every key's base hash code
/// by the seed, which changes the approx/bucket distribution via full-avalanche mixing. The winning
/// seed is stored in <see cref="HybleContext{TKey,TValue}.Seed" /> so the generated hash function can
/// reproduce the exact same mapping at query time by emitting <c>hash(key) * seed</c>.
/// Approx reduction uses Math.BigMul (multiply-high), matching the original Hyble algorithm.
/// </summary>
public sealed class HybleStructure<TKey, TValue> : IStructure<TKey, TValue, HybleContext<TKey, TValue>>
{
    private const uint MaxDisplacementBase = ushort.MaxValue - 64;
    private const uint DefaultKeysPerBucket = 5;
    private const uint DefaultDisplacementSearchStride = 57;
    private const uint MaxSeedAttempts = 256;
    private const ulong InitialSeed = 0x517CC1B727220A95; // odd — coprime to 2^64
    private const ulong SeedMult = 0x9E3779B97F4A7C15; // odd — Fibonacci hashing constant, keeps seed odd via multiply

    private readonly HashData _hashData;

    internal HybleStructure(HashData hashData)
    {
        _hashData = hashData;
    }

    public HybleContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        ReadOnlySpan<TKey> keySpan = keys.Span;
        ReadOnlySpan<TValue> valueSpan = values.Span;
        int keyCount = keySpan.Length;
        ulong[] baseHashCodes = _hashData.HashCodes;

        ulong seed = InitialSeed;
        for (uint attempt = 0; attempt < MaxSeedAttempts; attempt++)
        {
            // seed is always odd (InitialSeed is odd, SeedMult is odd, odd*odd=odd always),
            // so it is always coprime to 2^64 — guaranteeing a bijective multiplier.
            ulong[] seededHashes = new ulong[keyCount];
            for (int i = 0; i < keyCount; i++)
                unchecked { seededHashes[i] = baseHashCodes[i] * seed; }

            if (TryBuild(keySpan, valueSpan, seededHashes, seed, out HybleContext<TKey, TValue>? result))
                return result;

            unchecked { seed *= SeedMult; }
        }

        throw new InvalidOperationException($"Hyble: failed to construct a displacement table after {MaxSeedAttempts} attempts.");
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];

    private static bool TryBuild(ReadOnlySpan<TKey> keySpan, ReadOnlySpan<TValue> valueSpan, ulong[] hashes, ulong seed, out HybleContext<TKey, TValue>? result)
    {
        int keyCount = keySpan.Length;
        uint numKeys = (uint)keyCount;

        if (!TryComputeApproxRange(numKeys, out uint approxRange))
        {
            result = null;
            return false;
        }

        if (!TryComputeBucketLayout(numKeys, DefaultKeysPerBucket, out uint bucketCount, out uint bucketMask))
        {
            result = null;
            return false;
        }

        // Step 1: Hash and classify keys into buckets
        uint[] approxs = new uint[keyCount];
        int[] bucketsByKey = new int[keyCount];
        int[] bucketCounts = new int[bucketCount];

        for (int i = 0; i < keyCount; i++)
        {
            (uint approx, int bucket) = ToApproxBucket(hashes[i], approxRange, bucketMask);
            approxs[i] = approx;
            bucketsByKey[i] = bucket;
            bucketCounts[bucket]++;
        }

        // Step 2: Build prefix-sum index for bucket membership
        int[] bucketStarts = new int[bucketCount + 1];
        int[] bucketOffsets = new int[bucketCount];
        int[] bucketOrder = new int[bucketCount];
        int[] bucketKeyIndices = new int[keyCount];

        bucketStarts[0] = 0;
        for (int i = 0; i < (int)bucketCount; i++)
        {
            bucketStarts[i + 1] = bucketStarts[i] + bucketCounts[i];
            bucketOffsets[i] = bucketStarts[i];
            bucketOrder[i] = i;
        }

        for (int i = 0; i < keyCount; i++)
        {
            int bucket = bucketsByKey[i];
            int offset = bucketOffsets[bucket]++;
            bucketKeyIndices[offset] = i;
        }

        // Step 3: Check for approx collisions within buckets
        if (HasApproxCollision(bucketStarts, bucketCounts, bucketKeyIndices, approxs))
        {
            result = null;
            return false;
        }

        // Step 4: Sort buckets by size descending
        Array.Sort(bucketOrder, (a, b) =>
        {
            int sizeCompare = bucketCounts[b].CompareTo(bucketCounts[a]);
            return sizeCompare != 0 ? sizeCompare : a.CompareTo(b);
        });

        // Step 5: Find displacements
        ushort[] displacements = new ushort[bucketCount];
        ulong bitmapBits = approxRange + (ulong)ushort.MaxValue;
        int bitmapByteLength = (int)((bitmapBits + 7) / 8) + 7; // +7 padding for 8-byte ReadMask reads near the end
        byte[] freeBitmap = new byte[bitmapByteLength];
        for (int i = 0; i < freeBitmap.Length; i++)
            freeBitmap[i] = byte.MaxValue;

        for (int order = 0; order < bucketOrder.Length; order++)
        {
            int bucket = bucketOrder[order];
            int size = bucketCounts[bucket];

            if (size == 0)
                continue;

            if (!TryFindDisplacement(bucket, bucketStarts, bucketCounts, bucketKeyIndices, approxs, freeBitmap, DefaultDisplacementSearchStride, out ushort displacement))
            {
                result = null;
                return false;
            }

            displacements[bucket] = displacement;
            MarkBucketAsUsed(bucket, bucketStarts, bucketCounts, bucketKeyIndices, approxs, displacement, freeBitmap);
        }

        // Step 6: Compute total table size (max index + 1)
        uint maxIndex = 0;
        for (int i = 0; i < keyCount; i++)
        {
            int bucket = bucketsByKey[i];
            uint index = approxs[i] + displacements[bucket];
            if (index > maxIndex)
                maxIndex = index;
        }

        uint tableSize = maxIndex + 1;

        // Step 7: Scatter keys into the output array
        KeyValuePair<TKey, ulong>[] pairs = new KeyValuePair<TKey, ulong>[tableSize];
        TValue[]? denseValues = valueSpan.IsEmpty ? null : new TValue[tableSize];

        if (tableSize != (uint)keyCount)
        {
            // There are empty slots. Fill them with a valid placeholder key so that generated code
            // for languages like C++ and Rust always has a non-null value at every position.
            // Key comparison alone rejects non-members, so the placeholder key just needs to be
            // a valid value — we reuse the first key.
            ulong sentinel = GetSentinel(hashes, keyCount);
            KeyValuePair<TKey, ulong> fillEntry = new KeyValuePair<TKey, ulong>(keySpan[0], sentinel);

            for (uint i = 0; i < tableSize; i++)
                pairs[i] = fillEntry;

            if (denseValues != null)
            {
                for (uint i = 0; i < tableSize; i++)
                    denseValues[i] = valueSpan[0];
            }
        }

        for (int i = 0; i < keyCount; i++)
        {
            int bucket = bucketsByKey[i];
            uint index = approxs[i] + displacements[bucket];
            pairs[index] = new KeyValuePair<TKey, ulong>(keySpan[i], hashes[i]);

            if (denseValues != null)
                denseValues[index] = valueSpan[i];
        }

        result = new HybleContext<TKey, TValue>(pairs, displacements, approxRange, bucketMask, seed, denseValues);
        return true;
    }

    private static bool HasApproxCollision(int[] bucketStarts, int[] bucketCounts, int[] bucketKeyIndices, uint[] approxs)
    {
        for (int bucket = 0; bucket < bucketCounts.Length; bucket++)
        {
            int start = bucketStarts[bucket];
            int size = bucketCounts[bucket];

            for (int i = 1; i < size; i++)
            {
                uint approx = approxs[bucketKeyIndices[start + i]];

                for (int j = 0; j < i; j++)
                {
                    if (approxs[bucketKeyIndices[start + j]] == approx)
                        return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindDisplacement(int bucket, int[] bucketStarts, int[] bucketCounts, int[] bucketKeyIndices, uint[] approxs, byte[] freeBitmap, uint stride, out ushort displacement)
    {
        int start = bucketStarts[bucket];
        int size = bucketCounts[bucket];

        for (uint displacementBase = 0; displacementBase <= MaxDisplacementBase; displacementBase += stride)
        {
            ulong globalFreeMask = ulong.MaxValue;

            for (int i = 0; i < size; i++)
            {
                int keyIndex = bucketKeyIndices[start + i];
                int bitIndex = checked((int)(approxs[keyIndex] + displacementBase));
                globalFreeMask &= ReadMask(freeBitmap, bitIndex);

                if (globalFreeMask == 0)
                    break;
            }

            if (globalFreeMask == 0)
                continue;

            displacement = (ushort)(displacementBase + (uint)BitOperations.TrailingZeroCount(globalFreeMask));
            return true;
        }

        displacement = 0;
        return false;
    }

    private static void MarkBucketAsUsed(int bucket, int[] bucketStarts, int[] bucketCounts, int[] bucketKeyIndices, uint[] approxs, ushort displacement, byte[] freeBitmap)
    {
        int start = bucketStarts[bucket];
        int size = bucketCounts[bucket];

        for (int i = 0; i < size; i++)
        {
            int keyIndex = bucketKeyIndices[start + i];
            int index = checked((int)(approxs[keyIndex] + displacement));
            ResetBit(freeBitmap, index);
        }
    }

    private static (uint approx, int bucket) ToApproxBucket(ulong hash, uint approxRange, uint bucketMask)
    {
        // Use multiply-high reduction matching the original Hyble algorithm (Reduce64).
        // This maps [0, 2^64) to [0, approxRange) uniformly without division.
        uint approx = Reduce64(hash, approxRange);
        int bucket = (int)(hash & bucketMask);
        return (approx, bucket);
    }

    /// <summary>Maps a 64-bit hash uniformly to [0, range) using the high 64 bits of hash * range.</summary>
    private static uint Reduce64(ulong hash, uint range)
    {
#if NET5_0_OR_GREATER
        return (uint)Math.BigMul(hash, range, out _);
#else

        // Manual 128-bit multiply: split hash into two 32-bit halves, compute high word of product.
        ulong lo = (uint)hash;
        ulong hi = hash >> 32;
        ulong loProduct = lo * range;
        ulong hiProduct = hi * range;
        ulong carry = (loProduct >> 32) + (uint)hiProduct;
        return (uint)((carry >> 32) + (hiProduct >> 32));
#endif
    }

    private static ulong ReadMask(byte[] bitmap, int bitIndex)
    {
        int byteIndex = bitIndex >> 3;
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(bitmap.AsSpan(byteIndex, sizeof(ulong)));
        return value >> (bitIndex & 7);
    }

    private static void ResetBit(byte[] bitmap, int index) => bitmap[index >> 3] &= unchecked((byte)~(1 << (index & 7)));

    private static bool TryComputeBucketLayout(uint numKeys, uint keysPerBucket, out uint bucketCount, out uint bucketMask)
    {
        uint requested = DivCeil(numKeys, keysPerBucket);
        bucketCount = RoundUpToPowerOf2(requested);

        if (bucketCount == 0 || bucketCount < requested || bucketCount > int.MaxValue)
        {
            bucketMask = 0;
            return false;
        }

        bucketMask = bucketCount - 1;
        return true;
    }

    private static uint RoundUpToPowerOf2(uint value)
    {
        if (value <= 1)
            return 1;

        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    private static bool TryComputeApproxRange(uint numKeys, out uint approxRange)
    {
        uint percent = DivCeil(numKeys, 100);
        uint coeff = Math.Min(DivCeil(numKeys, 1_000_000), 5u);
        ulong approxRange64 = numKeys + ((ulong)coeff * percent);

        if (approxRange64 < numKeys || approxRange64 > int.MaxValue - ushort.MaxValue)
        {
            approxRange = 0;
            return false;
        }

        approxRange = (uint)approxRange64;
        return true;
    }

    private static uint DivCeil(uint a, uint b) => ((a + b) - 1) / b;

    private static ulong GetSentinel(ulong[] hashCodes, int count)
    {
        // Fast path: ulong.MaxValue is very unlikely to appear.
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            if (hashCodes[i] == ulong.MaxValue)
            {
                found = true;
                break;
            }
        }

        if (!found)
            return ulong.MaxValue;

        // Compute min/max of the actual hash values to find a boundary sentinel.
        ulong min = ulong.MaxValue;
        ulong max = ulong.MinValue;

        for (int i = 0; i < count; i++)
        {
            if (hashCodes[i] < min) min = hashCodes[i];
            if (hashCodes[i] > max) max = hashCodes[i];
        }

        if (max != ulong.MaxValue)
            return max + 1;

        if (min != 0)
            return min - 1;

        // Exhaustive search (extremely rare — all 64-bit values are used).
        ulong candidate = 1;
        while (true)
        {
            bool collision = false;

            for (int i = 0; i < count; i++)
            {
                if (hashCodes[i] == candidate)
                {
                    collision = true;
                    break;
                }
            }

            if (!collision)
                return candidate;

            candidate++;

            if (candidate == 0)
                throw new InvalidOperationException("Unable to find a sentinel hash value.");
        }
    }
}