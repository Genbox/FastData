using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashTableStructure<TKey, TValue>(HashData hashData) : IStructure<TKey, TValue, HashTableContext<TKey, TValue>>
{
    public HashTableContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        ReadOnlySpan<TKey> keySpan = keys.Span;
        ulong size = (ulong)(keySpan.Length * hashData.CapacityFactor);

        int[] buckets = new int[size];
        HashTableEntry<TKey>[] entries = new HashTableEntry<TKey>[keySpan.Length];

        for (int i = 0; i < keySpan.Length; i++)
        {
            ulong hashCode = hashData.HashCodes[i];
            ref int bucket = ref buckets[hashCode % size];

            ref HashTableEntry<TKey> entry = ref entries[i];
            entry.Hash = hashCode;
            entry.Next = bucket - 1;
            entry.Key = keySpan[i];
            bucket = i + 1;
        }

        return new HashTableContext<TKey, TValue>(buckets, entries, !typeof(TKey).UsesIdentityHash(), values);
    }
}