using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class HashTableCompactStructure<TKey, TValue> : IStructure<TKey, TValue, HashTableCompactContext<TKey, TValue>>
{
    private readonly HashData _hashData;

    internal HashTableCompactStructure(HashData hashData)
    {
        _hashData = hashData;
    }

    public HashTableCompactContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        ReadOnlySpan<TKey> keySpan = keys.Span;
        ReadOnlySpan<TValue> valueSpan = values.Span;
        ulong size = (ulong)(keySpan.Length * _hashData.CapacityFactor);

        int[] bucketCounts = new int[size];

        for (int i = 0; i < keySpan.Length; i++)
        {
            ulong hashCode = _hashData.HashCodes[i];
            bucketCounts[hashCode % size]++;
        }

        int[] bucketStarts = new int[size];
        int position = 0;

        for (int i = 0; i < bucketCounts.Length; i++)
        {
            bucketStarts[i] = position;
            position += bucketCounts[i];
        }

        HashTableCompactEntry<TKey>[] entries = new HashTableCompactEntry<TKey>[keySpan.Length];
        TValue[]? denseValues = values.IsEmpty ? null : new TValue[keySpan.Length];
        int[] bucketOffsets = new int[size];

        for (int i = 0; i < keySpan.Length; i++)
        {
            ulong hashCode = _hashData.HashCodes[i];
            ulong bucket = hashCode % size;
            int index = bucketStarts[bucket] + bucketOffsets[bucket]++;

            entries[index] = new HashTableCompactEntry<TKey>(hashCode, keySpan[i]);

            if (denseValues != null)
                denseValues[index] = valueSpan[i];
        }

        return new HashTableCompactContext<TKey, TValue>(bucketStarts, bucketCounts, entries, !Type.GetTypeCode(typeof(TKey)).UsesIdentityHash(), denseValues);
    }
}