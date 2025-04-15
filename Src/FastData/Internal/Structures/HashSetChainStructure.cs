using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Contexts.Misc;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetChainStructure : IHashStructure
{
    public bool TryCreate(object[] data, HashFunc hash, out IContext? context)
    {
        int[] buckets = new int[data.Length];
        HashSetEntry[] entries = new HashSetEntry[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            object value = data[i];
            uint hashCode = hash(value);
            ref int bucket = ref buckets[hashCode % data.Length];

            ref HashSetEntry entry = ref entries[i];
            entry.Hash = hashCode;
            entry.Next = bucket - 1;
            entry.Value = value;
            bucket = i + 1;
        }

        context = new HashSetChainContext(data, buckets, entries);
        return true;
    }
}