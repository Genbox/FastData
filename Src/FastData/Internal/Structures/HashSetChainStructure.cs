using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;
using Genbox.FastData.Models.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetChainStructure : IHashStructure
{
    public bool TryCreate(object[] data, DataType dataType, DataProperties props, FastDataConfig config, Func<object, uint> hash, out IContext? context)
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

    public double[] Emulate(object[] data, uint capacity, HashFunc hashFunc, EqualFunc equalFunc) => EmulateInternal(data, capacity, hashFunc, equalFunc);

    internal static double[] EmulateInternal(object[] data, uint capacity, HashFunc hashFunc, EqualFunc equalFunc)
    {
        //Note: FastSet does not call equals on elements
        FastSet set = new FastSet(capacity, hashFunc, equalFunc);

        int collisions = 0;
        foreach (string str in data)
        {
            if (!set.Add(str))
                collisions++;
        }

        //TODO: set.MinVariance, set.MaxVariance
        return [capacity / (double)collisions];
    }
}