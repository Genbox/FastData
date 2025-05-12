using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Contexts.Misc;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetChainStructure<T> : IHashStructure<T>
{
    public bool TryCreate(T[] data, HashFunc<T> hash, out IContext? context)
    {
        int[] buckets = new int[data.Length];
        HashSetEntry<T>[] entries = new HashSetEntry<T>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            T value = data[i];
            ulong hashCode = hash(value);
            ref int bucket = ref buckets[hashCode % (uint)data.Length];

            ref HashSetEntry<T> entry = ref entries[i];
            entry.Hash = hashCode;
            entry.Next = bucket - 1;
            entry.Value = value;
            bucket = i + 1;
        }

        context = new HashSetChainContext<T>(data, buckets, entries);
        return true;
    }
}