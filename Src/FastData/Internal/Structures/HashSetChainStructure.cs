using System.Diagnostics;
using System.Runtime.InteropServices;
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
        uint capacity = (uint)(data.Length * config.CapacityFactor);

        long ticks = Stopwatch.GetTimestamp();
        (int occupied, int minVariance, int maxVariance) = Emulate(data, capacity, hashFunc);
        ticks = Stopwatch.GetTimestamp() - ticks;

        double normOccu = (occupied / (double)capacity) * config.FillWeight;
        double normTime = (1.0 / (1.0 + ((double)ticks / 1000))) * config.TimeWeight;

        int variance = maxVariance - minVariance;
        double normVar = (1.0 / (1.0 + variance)) * config.VarianceWeight;

        cand.Fitness = (normOccu + normTime + normVar) / 3;

        cand.Metadata["Time"] = ticks;
        cand.Metadata["TimeNormalized"] = normTime;
        cand.Metadata["Occupied"] = occupied;
        cand.Metadata["OccupiedNormalized"] = normOccu;
        cand.Metadata["MinVariance"] = minVariance;
        cand.Metadata["MaxVariance"] = maxVariance;
        cand.Metadata["VarianceNormalized"] = normVar;
    }

    private static (int cccupied, int minVariance, int maxVariance) Emulate(object[] data, uint capacity, Func<string, uint> hashFunc)
    {
        FastSet set = new FastSet(capacity, hashFunc);

        int occupied = 0;
        foreach (string str in data)
        {
            if (set.Add(str))
                occupied++;
        }

        return (occupied, set.MinVariance, set.MaxVariance);
    }

    private ref struct FastSet(uint capacity, Func<string, uint> hashFunc)
    {
        private readonly int[] _buckets = new int[capacity];
        private readonly Entry[] _entries = new Entry[capacity];
        private int _count;

        //TODO: optimize
        public readonly int MinVariance => _buckets.Where(x => x != 0).Min();
        public readonly int MaxVariance => _buckets.Max();

        public bool Add(string value)
        {
            uint hashCode = hashFunc(value);
            ref int bucket = ref _buckets[hashCode % capacity];
            int i = bucket - 1;

            while (i >= 0)
            {
                Entry entry = _entries[i];

                if (entry.Hash == hashCode)
                    return false;

                i = entry.Next;
            }

            ref Entry newEntry = ref _entries[_count];
            newEntry.Hash = hashCode;
            newEntry.Next = bucket - 1;
            bucket = _count + 1;

            _count++;
            return true;
        }

        [StructLayout(LayoutKind.Auto)]
        private record struct Entry(uint Hash, int Next);
    }
}