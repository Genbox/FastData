using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetChainStructure<T>(HashData hashData) : IStructure<T, HashSetChainContext<T>>
{
    public HashSetChainContext<T> Create(ref ReadOnlySpan<T> data)
    {
        ulong size = (ulong)(data.Length * hashData.CapacityFactor);

        int[] buckets = new int[size];
        HashSetEntry<T>[] entries = new HashSetEntry<T>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            ulong hashCode = hashData.HashCodes[i];
            T value = data[i];
            ref int bucket = ref buckets[hashCode % size];

            ref HashSetEntry<T> entry = ref entries[i];
            entry.Hash = hashCode;
            entry.Next = bucket - 1;
            entry.Value = value;
            bucket = i + 1;
        }

        return new HashSetChainContext<T>(buckets, entries);
    }
}