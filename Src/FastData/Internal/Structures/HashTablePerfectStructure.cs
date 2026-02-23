using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashTablePerfectStructure<TKey, TValue>(HashData hashData) : IStructure<TKey, TValue, HashTablePerfectContext<TKey, TValue>>
{
    public HashTablePerfectContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (!hashData.HashCodesPerfect)
            throw new InvalidOperationException("HashSetPerfectStructure can only be created with a perfect hash function.");

        ReadOnlySpan<TKey> keySpan = keys.Span;
        ReadOnlySpan<TValue> valueSpan = values.Span;
        ulong size = (ulong)(keySpan.Length * hashData.CapacityFactor);
        bool hasEmptySlots = size != (ulong)keySpan.Length;
        bool storeHashCode = !typeof(TKey).UsesIdentityHash() || hasEmptySlots;
        ulong[] hashCodes = hashData.HashCodes;
        KeyValuePair<TKey, ulong>[] pairs = new KeyValuePair<TKey, ulong>[size];
        TValue[]? denseValues = values.IsEmpty ? null : new TValue[size];

        if (storeHashCode && hasEmptySlots)
        {
            ulong sentinel = GetSentinel(hashData, hashCodes, keySpan.Length);

            for (ulong i = 0; i < size; i++)
                pairs[i] = new KeyValuePair<TKey, ulong>(default!, sentinel);
        }

        //We need to reorder the data to match hashes
        for (int i = 0; i < keySpan.Length; i++)
        {
            ulong index = hashCodes[i] % size;
            pairs[index] = new KeyValuePair<TKey, ulong>(keySpan[i], hashCodes[i]);

            if (denseValues != null)
                denseValues[index] = valueSpan[i];
        }

        return new HashTablePerfectContext<TKey, TValue>(pairs, storeHashCode, denseValues);
    }

    private static ulong GetSentinel(HashData hashData, ulong[] hashCodes, int count)
    {
        if (hashData.MaxHashCode != ulong.MaxValue)
            return hashData.MaxHashCode + 1;

        if (hashData.MinHashCode != 0)
            return hashData.MinHashCode - 1;

        ulong candidate = 1;
        while (true)
        {
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (hashCodes[i] == candidate)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return candidate;

            candidate++;

            if (candidate == 0)
                throw new InvalidOperationException("Unable to find a sentinel hash value.");
        }
    }
}