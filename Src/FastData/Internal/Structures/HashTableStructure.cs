using System.Diagnostics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class HashTableStructure<TKey, TValue> : IStructure<TKey, TValue, HashTableContext<TKey, TValue>>
{
    private readonly HashData _hashData;

    internal HashTableStructure(HashData hashData)
    {
        _hashData = hashData;
    }

    public HashTableContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        Debug.Assert(!keys.IsEmpty, "HashTableStructure requires at least one key.");
        Debug.Assert(values.IsEmpty || values.Length == keys.Length, "HashTableStructure requires value count to match key count when values are present.");
        Debug.Assert(_hashData.CapacityFactor > 0, "HashTableStructure requires a positive capacity factor.");
        Debug.Assert(_hashData.HashCodes.Length >= keys.Length, "HashTableStructure requires one hash code per key.");
        Debug.Assert(_hashData.TableSize > 0, "HashTableStructure requires a positive bucket table size.");

        ReadOnlySpan<TKey> keySpan = keys.Span;
        int size = _hashData.TableSize;

        int[] buckets = new int[size];
        HashTableEntry<TKey>[] entries = new HashTableEntry<TKey>[keySpan.Length];

        for (int i = 0; i < keySpan.Length; i++)
        {
            ulong hashCode = _hashData.HashCodes[i];
            ref int bucket = ref buckets[(int)(hashCode % (uint)size)];

            ref HashTableEntry<TKey> entry = ref entries[i];
            entry.Hash = hashCode;
            entry.Next = bucket - 1;
            entry.Key = keySpan[i];
            bucket = i + 1;
        }

        return new HashTableContext<TKey, TValue>(buckets, entries, !Type.GetTypeCode(typeof(TKey)).UsesIdentityHash(), values);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}