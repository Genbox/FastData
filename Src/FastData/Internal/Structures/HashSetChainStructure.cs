using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Contexts.Misc;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetChainStructure<T>(HashData hashData) : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        int[] buckets = new int[data.Length];
        HashSetEntry<T>[] entries = new HashSetEntry<T>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            ulong hashCode = hashData.HashCodes[i];
            T value = data[i];
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