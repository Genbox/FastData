using System.Diagnostics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class HashTablePerfectStructure<TKey, TValue> : IStructure<TKey, TValue, HashTablePerfectContext<TKey, TValue>>
{
    private readonly HashData _hashData;

    internal HashTablePerfectStructure(HashData hashData)
    {
        _hashData = hashData;
    }

    public HashTablePerfectContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        Debug.Assert(!keys.IsEmpty, "HashTablePerfectStructure requires at least one key.");
        Debug.Assert(values.IsEmpty || values.Length == keys.Length, "HashTablePerfectStructure requires value count to match key count when values are present.");
        Debug.Assert(_hashData.CapacityFactor > 0, "HashTablePerfectStructure requires a positive capacity factor.");
        Debug.Assert(_hashData.HashCodes.Length >= keys.Length, "HashTablePerfectStructure requires one hash code per key.");
        Debug.Assert(_hashData.HashCodesPerfect, "HashTablePerfectStructure requires a perfect hash function.");
        Debug.Assert(_hashData.TableSize > 0, "HashTablePerfectStructure requires a positive table size.");

        if (!_hashData.HashCodesPerfect)
            throw new InvalidOperationException("HashSetPerfectStructure can only be created with a perfect hash function.");

        ReadOnlySpan<TKey> keySpan = keys.Span;
        ReadOnlySpan<TValue> valueSpan = values.Span;
        int size = _hashData.TableSize;
        bool hasEmptySlots = size != keySpan.Length;
        bool storeHashCode = !Type.GetTypeCode(typeof(TKey)).UsesIdentityHash() || hasEmptySlots;
        ulong[] hashCodes = _hashData.HashCodes;
        KeyValuePair<TKey, ulong>[] pairs = new KeyValuePair<TKey, ulong>[size];
        TValue[]? denseValues = values.IsEmpty ? null : new TValue[size];

        if (storeHashCode && hasEmptySlots)
        {
            ulong sentinel = GetSentinel(_hashData, hashCodes, keySpan.Length);

            for (int i = 0; i < size; i++)
                pairs[i] = new KeyValuePair<TKey, ulong>(default!, sentinel);
        }

        //We need to reorder the data to match hashes
        for (int i = 0; i < keySpan.Length; i++)
        {
            int index = (int)(hashCodes[i] % (uint)size);
            pairs[index] = new KeyValuePair<TKey, ulong>(keySpan[i], hashCodes[i]);

            if (denseValues != null)
                denseValues[index] = valueSpan[i];
        }

        return new HashTablePerfectContext<TKey, TValue>(pairs, storeHashCode, denseValues);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];

    private static ulong GetSentinel(HashData hashData, ulong[] hashCodes, int count)
    {
        Debug.Assert(count > 0, "Sentinel selection requires at least one hash code.");
        Debug.Assert(hashCodes.Length >= count, "Sentinel selection requires hashCodes to contain count entries.");

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