using System.Diagnostics;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Models;
using Genbox.FastData.Models.Misc;

namespace Genbox.FastData.Internal.Generators;

internal sealed class HashSetChainCode : IHashStructure
{
    public bool TryCreate(object[] data, KnownDataType dataType, DataProperties props, FastDataConfig config, Func<object, uint> hash, out IContext? context)
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
            entry.Next = bucket - 1; // Value in buckets is 1-based
            entry.Value = value;
            bucket = i + 1;
        }

        context = new HashSetChainContext(data, buckets, entries);
        return true;
    }

    public void RunSimulation<T>(object[] data, AnalyzerConfig config, ref Candidate<T> candidate) where T : struct, IHashSpec
    {
        // Generate a hash function from the spec
        Func<string, uint> hashFunc = candidate.Spec.GetFunction();
        int capacity = (int)(data.Length * config.CapacityFactor);

        long ticks = Stopwatch.GetTimestamp();
        (int occupied, double minVariance, double maxVariance) = Emulate(data, capacity, hashFunc);
        ticks = Stopwatch.GetTimestamp() - ticks;

        double normOccu = (occupied / (double)capacity) * config.FillWeight;
        double normTime = (1.0 / (1.0 + ((double)ticks / 1000))) * config.TimeWeight;

        candidate.Fitness = (normOccu + normTime) / 2;
        candidate.Metadata = [("Time/norm", ticks + "/" + normTime.ToString("N2")), ("Occupied/norm", occupied + "/" + normOccu.ToString("N2")), ("MinVariance", minVariance), ("MaxVariance", maxVariance)];
    }

    private static (int cccupied, double minVariance, double maxVariance) Emulate(object[] data, int capacity, Func<string, uint> hashFunc)
    {
        int[] buckets = new int[capacity];

        for (int i = 0; i < capacity; i++)
            buckets[hashFunc((string)data[i]) % buckets.Length]++;

        int occupied = 0;
        double minVariance = double.MaxValue;
        double maxVariance = double.MinValue;

        for (int i = 0; i < buckets.Length; i++)
        {
            int bucket = buckets[i];

            if (bucket > 0)
                occupied++;

            minVariance = Math.Min(minVariance, bucket);
            maxVariance = Math.Max(maxVariance, bucket);
        }

        return (occupied, minVariance, maxVariance);
    }
}