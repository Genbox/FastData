using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashTablePerfectStructure<TKey, TValue>(HashData hashData, DataType dataType) : IStructure<TKey, TValue, HashTablePerfectContext<TKey, TValue>>
{
    public HashTablePerfectContext<TKey, TValue> Create(TKey[] data, TValue[]? values)
    {
        if (!hashData.HashCodesPerfect)
            throw new InvalidOperationException("HashSetPerfectStructure can only be created with a perfect hash function.");

        ulong size = (ulong)(data.Length * hashData.CapacityFactor);

        ulong[] hashCodes = hashData.HashCodes;
        KeyValuePair<TKey, ulong>[] pairs = new KeyValuePair<TKey, ulong>[size];

        //We need to reorder the data to match hashes
        for (int i = 0; i < data.Length; i++)
            pairs[hashCodes[i] % size] = new KeyValuePair<TKey, ulong>(data[i], hashCodes[i]);

        return new HashTablePerfectContext<TKey, TValue>(pairs, !dataType.IsIdentityHash(), values);
    }
}