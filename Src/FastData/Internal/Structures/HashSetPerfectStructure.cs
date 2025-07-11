using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetPerfectStructure<T>(HashData hashData, DataType dataType) : IStructure<T, HashSetPerfectContext<T>>
{
    public HashSetPerfectContext<T> Create(T[] data)
    {
        if (!hashData.HashCodesPerfect)
            throw new InvalidOperationException("HashSetPerfectStructure can only be created with a perfect hash function.");

        ulong size = (ulong)(data.Length * hashData.CapacityFactor);

        ulong[] hashCodes = hashData.HashCodes;
        KeyValuePair<T, ulong>[] pairs = new KeyValuePair<T, ulong>[size];

        //We need to reorder the data to match hashes
        for (int i = 0; i < data.Length; i++)
            pairs[hashCodes[i] % size] = new KeyValuePair<T, ulong>(data[i], hashCodes[i]);

        return new HashSetPerfectContext<T>(pairs, !dataType.IsIdentityHash());
    }
}