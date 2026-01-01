using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BloomFilterStructure<TKey, TValue>(HashData hashData) : IStructure<TKey, TValue, BloomFilterContext<TKey, TValue>>
{
    private const int BitsPerKey = 10;

    public BloomFilterContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        int capacity = keys.Length;
        int bits = checked(capacity * BitsPerKey);
        int buckets = bits / 64;

        uint length = (uint)(buckets + sizeof(ulong));
        ulong[] bitset = new ulong[length];
        ulong[] hashCodes = hashData.HashCodes;

        for (int i = 0; i < capacity; i++)
        {
            ulong hash = hashCodes[i];
            uint index = (uint)(hash % length);
            uint shift1 = unchecked((uint)hash & 63u);
            uint shift2 = unchecked((uint)(hash >> 8) & 63u);
            ulong mask = (1UL << (int)shift1) | (1UL << (int)shift2);
            bitset[(int)index] |= mask;
        }

        return new BloomFilterContext<TKey, TValue>(bitset);
    }
}