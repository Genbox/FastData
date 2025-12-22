using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashTablePerfectStructure<TKey, TValue>(HashData hashData, KeyType keyType) : IStructure<TKey, TValue, HashTablePerfectContext<TKey, TValue>>
{
    public HashTablePerfectContext<TKey, TValue> Create(TKey[] keys, TValue[]? values)
    {
        if (!hashData.HashCodesPerfect)
            throw new InvalidOperationException("HashSetPerfectStructure can only be created with a perfect hash function.");

        ulong size = (ulong)(keys.Length * hashData.CapacityFactor);

        ulong[] hashCodes = hashData.HashCodes;
        KeyValuePair<TKey, ulong>[] pairs = new KeyValuePair<TKey, ulong>[size];
        TValue[]? denseValues = values == null ? null : new TValue[size];

        //We need to reorder the data to match hashes
        for (int i = 0; i < keys.Length; i++)
        {
            ulong index = hashCodes[i] % size;
            pairs[index] = new KeyValuePair<TKey, ulong>(keys[i], hashCodes[i]);

            if (denseValues != null)
                denseValues[index] = values![i];
        }

        return new HashTablePerfectContext<TKey, TValue>(pairs, !keyType.IsIdentityHash(), denseValues);
    }
}