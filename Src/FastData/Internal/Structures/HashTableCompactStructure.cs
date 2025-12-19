using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashTableCompactStructure<TKey, TValue>(HashData hashData, KeyType keyType) : IStructure<TKey, TValue, HashTableCompactContext<TKey, TValue>>
{
    public HashTableCompactContext<TKey, TValue> Create(TKey[] keys, TValue[]? values)
    {
        ulong size = (ulong)(keys.Length * hashData.CapacityFactor);

        int[] bucketCounts = new int[size];

        for (int i = 0; i < keys.Length; i++)
        {
            ulong hashCode = hashData.HashCodes[i];
            bucketCounts[hashCode % size]++;
        }

        int[] bucketStarts = new int[size];
        int position = 0;

        for (int i = 0; i < bucketCounts.Length; i++)
        {
            bucketStarts[i] = position;
            position += bucketCounts[i];
        }

        HashTableCompactEntry<TKey>[] entries = new HashTableCompactEntry<TKey>[keys.Length];
        TValue[]? denseValues = values == null ? null : new TValue[keys.Length];
        int[] bucketOffsets = new int[size];

        for (int i = 0; i < keys.Length; i++)
        {
            ulong hashCode = hashData.HashCodes[i];
            ulong bucket = hashCode % size;
            int index = bucketStarts[bucket] + bucketOffsets[bucket]++;

            entries[index] = new HashTableCompactEntry<TKey>(hashCode, keys[i]);

            if (denseValues != null)
                denseValues[index] = values![i];
        }

        return new HashTableCompactContext<TKey, TValue>(bucketStarts, bucketCounts, entries, !keyType.IsIdentityHash(), denseValues);
    }
}
