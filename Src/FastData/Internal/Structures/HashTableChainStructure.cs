using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashTableChainStructure<TKey, TValue>(HashData hashData, DataType dataType) : IStructure<TKey, TValue, HashTableChainContext<TKey, TValue>>
{
    public HashTableChainContext<TKey, TValue> Create(TKey[] data, ValueSpec<TValue>? valueSpec)
    {
        ulong size = (ulong)(data.Length * hashData.CapacityFactor);

        int[] buckets = new int[size];
        HashTableEntry<TKey>[] entries = new HashTableEntry<TKey>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            ulong hashCode = hashData.HashCodes[i];
            TKey value = data[i];
            ref int bucket = ref buckets[hashCode % size];

            ref HashTableEntry<TKey> entry = ref entries[i];
            entry.Hash = hashCode;
            entry.Next = bucket - 1;
            entry.Value = value;
            bucket = i + 1;
        }

        return new HashTableChainContext<TKey, TValue>(buckets, entries, !dataType.IsIdentityHash(), valueSpec);
    }
}