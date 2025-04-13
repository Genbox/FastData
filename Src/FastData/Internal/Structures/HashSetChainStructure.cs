using System.Diagnostics;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
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

    public void RunSimulation<T>(object[] data, AnalyzerConfig config, ref Candidate<T> cand) where T : struct, IHashSpec
    {
        // Generate a hash function from the spec
        Func<string, uint> hashFunc = cand.Spec.GetFunction();
        Func<string, string, bool> equalFunc = cand.Spec.GetEqualFunction();
        uint capacity = (uint)(data.Length * config.CapacityFactor);

        long ticks = Stopwatch.GetTimestamp();
        (int collisions, int minVariance, int maxVariance) = Emulate(data, capacity, hashFunc, equalFunc);
        ticks = Stopwatch.GetTimestamp() - ticks;

        double normColl = (1.0 - ((double)collisions / capacity)) * config.FillWeight;
        double normTime = (1.0 / (1.0 + ((double)ticks / 1000))) * config.TimeWeight;

        //TODO: Implement variance
        cand.Fitness = (normColl + normTime) / 2;

        cand.Metadata["Time"] = ticks;
        cand.Metadata["TimeNormalized"] = normTime;
        cand.Metadata["Collisions"] = collisions;
        cand.Metadata["CollisionsNormalized"] = normColl;
        cand.Metadata["MinVariance"] = minVariance;
        cand.Metadata["MaxVariance"] = maxVariance;
    }

    private static (int cccupied, int minVariance, int maxVariance) Emulate(object[] data, uint capacity, Func<string, uint> hashFunc, Func<string, string, bool> equalFunc)
    {
        //Note: FastSet does not call equals on elements
        FastSet set = new FastSet(capacity, hashFunc, equalFunc);

        int collisions = 0;
        foreach (string str in data)
        {
            if (!set.Add(str))
                collisions++;
        }

        return (collisions, set.MinVariance, set.MaxVariance);
    }
}