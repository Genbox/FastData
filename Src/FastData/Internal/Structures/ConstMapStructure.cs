using System.Diagnostics;
using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

/// <summary>
/// Builds an exact map using the binary-fuse construction. The XOR table recovers a candidate
/// ordinal; generated code then compares the complete key before returning membership or a value.
/// </summary>
public sealed class ConstMapStructure<TKey, TValue> : IStructure<TKey, TValue, ConstMapContext<TKey, TValue>>
{
    private const int MaxIterations = 100;
    private const uint MaxSegmentLength = 262_144;

    private readonly HashData _hashData;

    internal ConstMapStructure(HashData hashData)
    {
        _hashData = hashData;
    }

    public StructureCapability SupportedCapabilities => StructureCapability.Membership | StructureCapability.KeyValueLookup;

    public ConstMapContext<TKey, TValue>? Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        Debug.Assert(!keys.IsEmpty, "ConstMapStructure requires at least one key.");
        Debug.Assert(values.IsEmpty || values.Length == keys.Length, "ConstMapStructure requires value count to match key count when values are present.");
        Debug.Assert(_hashData.HashCodes.Length >= keys.Length, "ConstMapStructure requires one hash code per key.");

        uint size = (uint)keys.Length;
        if (!TryInitializeParameters(size, out uint segmentLength, out uint segmentCount, out uint segmentCountLength, out int arrayLength))
            return null;

        uint[] data = new uint[arrayLength];
        uint[] alone = new uint[arrayLength];
        byte[] t2Count = new byte[arrayLength];
        ulong[] t2Hash = new ulong[arrayLength];
        uint[] t2Id = new uint[arrayLength];
        byte[] reverseH = new byte[keys.Length];
        ulong[] reverseOrder = new ulong[keys.Length + 1];
        uint[] reverseIds = new uint[keys.Length];
        byte[] occupied = new byte[keys.Length + 1];
        occupied[keys.Length] = 1;

        ulong rngCounter = 1;
        ulong seed = SplitMix64(ref rngCounter);
        uint[] startPos = [];

        for (int iteration = 1; iteration <= MaxIterations; iteration++)
        {
            if (size > 4 && size < 1_000_000)
            {
                switch (iteration % 4)
                {
                    case 2:
                        segmentLength /= 2;
                        segmentCount = (segmentCount * 2) + 2;
                        segmentCountLength = segmentCount * segmentLength;
                        break;
                    case 3:
                        segmentLength *= 2;
                        segmentCount = (segmentCount / 2) - 1;
                        segmentCountLength = segmentCount * segmentLength;
                        break;
                }
            }

            uint segmentLengthMask = segmentLength - 1;
            int blockBits = 1;
            while (1u << blockBits < segmentCount)
                blockBits++;

            int blockCount = 1 << blockBits;
            if (startPos.Length < blockCount)
                startPos = new uint[blockCount];

            for (int i = 0; i < blockCount; i++)
                startPos[i] = (uint)(((ulong)(uint)i * size) >> blockBits);

            for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
            {
                ulong hash = MixSplit(_hashData.HashCodes[keyIndex], seed);
                uint segmentIndex = (uint)(hash >> (64 - blockBits));

                while (occupied[startPos[segmentIndex]] != 0)
                    segmentIndex = (segmentIndex + 1) & (uint)(blockCount - 1);

                uint position = startPos[segmentIndex]++;
                reverseOrder[position] = hash;
                reverseIds[position] = (uint)keyIndex;
                occupied[position] = 1;
            }

            bool attemptInvalid = false;

            for (uint i = 0; i < size; i++)
            {
                ulong hash = reverseOrder[i];
                uint id = reverseIds[i];
                GetHashFromHash(hash, segmentLength, segmentLengthMask, segmentCountLength, out uint index0, out uint index1, out uint index2);

                t2Count[index0] = unchecked((byte)(t2Count[index0] + 4));
                t2Hash[index0] ^= hash;
                t2Id[index0] ^= id;

                t2Count[index1] = unchecked((byte)(t2Count[index1] + 4));
                t2Count[index1] ^= 1;
                t2Hash[index1] ^= hash;
                t2Id[index1] ^= id;

                t2Count[index2] = unchecked((byte)(t2Count[index2] + 4));
                t2Count[index2] ^= 2;
                t2Hash[index2] ^= hash;
                t2Id[index2] ^= id;

                // Degree is stored in six bits. Retry before duplicate detection once it wraps.
                if (t2Count[index0] < 4 || t2Count[index1] < 4 || t2Count[index2] < 4)
                {
                    attemptInvalid = true;
                    break;
                }

                if ((t2Hash[index0] == 0 && t2Count[index0] == 8) ||
                    (t2Hash[index1] == 0 && t2Count[index1] == 8) ||
                    (t2Hash[index2] == 0 && t2Count[index2] == 8))
                    return null;
            }

            if (!attemptInvalid)
            {
                int queueSize = 0;
                for (uint i = 0; i < (uint)arrayLength; i++)
                {
                    if (t2Count[i] >> 2 == 1)
                        alone[queueSize++] = i;
                }

                uint stackSize = 0;
                while (queueSize > 0)
                {
                    uint index = alone[--queueSize];
                    if (t2Count[index] >> 2 != 1)
                        continue;

                    ulong hash = t2Hash[index];
                    uint id = t2Id[index];
                    byte found = (byte)(t2Count[index] & 3);
                    Debug.Assert(found < 3, "A singleton edge must identify one of three hash positions.");

                    reverseH[stackSize] = found;
                    reverseOrder[stackSize] = hash;
                    reverseIds[stackSize] = id;
                    stackSize++;

                    GetHashFromHash(hash, segmentLength, segmentLengthMask, segmentCountLength, out uint index0, out uint index1, out uint index2);

                    switch (found)
                    {
                        case 0:
                            RemoveEdge(index1, 1, hash, id, t2Count, t2Hash, t2Id, alone, ref queueSize);
                            RemoveEdge(index2, 2, hash, id, t2Count, t2Hash, t2Id, alone, ref queueSize);
                            break;
                        case 1:
                            RemoveEdge(index0, 0, hash, id, t2Count, t2Hash, t2Id, alone, ref queueSize);
                            RemoveEdge(index2, 2, hash, id, t2Count, t2Hash, t2Id, alone, ref queueSize);
                            break;
                        case 2:
                            RemoveEdge(index0, 0, hash, id, t2Count, t2Hash, t2Id, alone, ref queueSize);
                            RemoveEdge(index1, 1, hash, id, t2Count, t2Hash, t2Id, alone, ref queueSize);
                            break;
                    }
                }

                if (stackSize == size)
                {
                    for (int i = keys.Length - 1; i >= 0; i--)
                    {
                        ulong hash = reverseOrder[i];
                        uint id = reverseIds[i];
                        GetHashFromHash(hash, segmentLength, segmentLengthMask, segmentCountLength, out uint index0, out uint index1, out uint index2);

                        switch (reverseH[i])
                        {
                            case 0:
                                data[index0] = id ^ data[index1] ^ data[index2];
                                break;
                            case 1:
                                data[index1] = id ^ data[index0] ^ data[index2];
                                break;
                            case 2:
                                data[index2] = id ^ data[index0] ^ data[index1];
                                break;
                        }
                    }

                    return new ConstMapContext<TKey, TValue>(keys, values, data, seed, segmentLength, segmentCountLength);
                }
            }

            Array.Clear(reverseOrder, 0, keys.Length);
            Array.Clear(occupied, 0, keys.Length);
            Array.Clear(t2Count, 0, t2Count.Length);
            Array.Clear(t2Hash, 0, t2Hash.Length);
            Array.Clear(t2Id, 0, t2Id.Length);
            seed = SplitMix64(ref rngCounter);
        }

        return null;
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];

    private static void RemoveEdge(uint index, byte position, ulong hash, uint id, byte[] counts, ulong[] hashes, uint[] ids, uint[] queue, ref int queueSize)
    {
        if (counts[index] >> 2 == 2)
            queue[queueSize++] = index;

        counts[index] -= 4;
        counts[index] ^= position;
        hashes[index] ^= hash;
        ids[index] ^= id;
    }

    private static void GetHashFromHash(ulong hash, uint segmentLength, uint segmentLengthMask, uint segmentCountLength, out uint h0, out uint h1, out uint h2)
    {
        h0 = Reduce64(hash, segmentCountLength);
        h1 = (h0 + segmentLength) ^ (unchecked((uint)(hash >> 18)) & segmentLengthMask);
        h2 = (h0 + (2 * segmentLength)) ^ (unchecked((uint)hash) & segmentLengthMask);
    }

    private static uint Reduce64(ulong hash, uint range)
    {
#if NET5_0_OR_GREATER
        return (uint)Math.BigMul(hash, range, out _);
#else
        unchecked
        {
            ulong lo = (uint)hash;
            ulong hi = hash >> 32;
            ulong loProduct = lo * range;
            ulong hiProduct = hi * range;
            ulong carry = (loProduct >> 32) + (uint)hiProduct;
            return (uint)((carry >> 32) + (hiProduct >> 32));
        }
#endif
    }

    private static bool TryInitializeParameters(uint size, out uint segmentLength, out uint segmentCount, out uint segmentCountLength, out int arrayLength)
    {
        int exponent = (int)Math.Floor((Math.Log(size) / Math.Log(3.33)) + 2.25);
        segmentLength = exponent >= 18 ? MaxSegmentLength : 1u << exponent;

        double capacityValue = size > 1 ? Math.Round(size * CalculateSizeFactor(size), MidpointRounding.AwayFromZero) : 0;
        if (capacityValue > uint.MaxValue)
        {
            segmentCount = 0;
            segmentCountLength = 0;
            arrayLength = 0;
            return false;
        }

        uint capacity = (uint)capacityValue;
        ulong totalSegmentCount = (((ulong)capacity + segmentLength) - 1) / segmentLength;
        if (totalSegmentCount < 3)
            totalSegmentCount = 3;

        ulong arrayLengthValue = totalSegmentCount * segmentLength;
        if (arrayLengthValue > int.MaxValue)
        {
            segmentCount = 0;
            segmentCountLength = 0;
            arrayLength = 0;
            return false;
        }

        segmentCount = (uint)totalSegmentCount - 2;
        segmentCountLength = segmentCount * segmentLength;
        arrayLength = (int)arrayLengthValue;
        return true;
    }

    private static double CalculateSizeFactor(uint size) => Math.Max(1.125, 0.875 + ((0.25 * Math.Log(1_000_000)) / Math.Log(size)));

    private static ulong MixSplit(ulong key, ulong seed) => Murmur64(unchecked(key + seed));

    private static ulong Murmur64(ulong hash)
    {
        unchecked
        {
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccd;
            hash ^= hash >> 33;
            hash *= 0xc4ceb9fe1a85ec53;
            hash ^= hash >> 33;
            return hash;
        }
    }

    private static ulong SplitMix64(ref ulong seed)
    {
        unchecked
        {
            seed += 0x9E3779B97F4A7C15;
            ulong value = seed;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EB;
            return value ^ (value >> 31);
        }
    }
}