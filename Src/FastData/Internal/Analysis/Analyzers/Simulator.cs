using System.Diagnostics;
using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal class Simulator(object[] data, SimulatorConfig config)
{
    internal void Run<T>(ref Candidate<T> cand) where T : struct, IHashSpec
    {
        // Generate a hash function from the spec
        HashFunc hashFunc = cand.Spec.GetHashFunction();
        EqualFunc equalFunc = cand.Spec.GetEqualFunction();
        uint capacity = (uint)(data.Length * config.CapacityFactor);

        long ticks = Stopwatch.GetTimestamp();
        double[] results = Emulate(cand.Metadata, data, capacity, hashFunc, equalFunc);
        ticks = Stopwatch.GetTimestamp() - ticks;

        double normEmu = results.Average() * config.EmulationWeight;
        double normTime = 1.0 / (1.0 + ((double)ticks / 1000)) * config.TimeWeight;

        int count = 0;

        if (config.EmulationWeight > 0) count++;
        if (config.TimeWeight > 0) count++;

        cand.Fitness = (normEmu + normTime) / count;

        cand.Metadata["EmulationNormalized"] = normEmu;
        cand.Metadata["Time"] = ticks;
        cand.Metadata["TimeNormalized"] = normTime;
    }

    private static double[] Emulate(Dictionary<string, object> metadata, object[] data, uint capacity, HashFunc hashFunc, EqualFunc equalFunc)
    {
        //Note: FastSet does not call equals on elements
        FastSet<object> set = new FastSet<object>(capacity, hashFunc, equalFunc);

        int collisions = 0;
        foreach (string str in data)
        {
            if (!set.Add(str))
                collisions++;
        }

        metadata["Collisions"] = collisions;
        metadata["MinVariance"] = set.MinVariance;
        metadata["MaxVariance"] = set.MaxVariance;

        //TODO: Use MinVariance & MaxVariance
        return [(double)capacity / (collisions + 1)];
    }

    private ref struct FastSet<T>(uint capacity, HashFunc hashFunc, EqualFunc equalFunc)
    {
        private readonly int[] _buckets = new int[capacity];
        private readonly Entry[] _entries = new Entry[capacity];
        private int _count;

        //TODO: optimize
        public readonly int MinVariance => _buckets.Where(x => x != 0).Min();
        public readonly int MaxVariance => _buckets.Max();

        public bool Add(T value)
        {
            uint hashCode = hashFunc(value);
            ref int bucket = ref _buckets[hashCode % capacity];
            int i = bucket - 1;

            while (i >= 0)
            {
                Entry entry = _entries[i];

                if (entry.Hash == hashCode && equalFunc(entry.Value, value))
                    return false;

                i = entry.Next;
            }

            ref Entry newEntry = ref _entries[_count];
            newEntry.Hash = hashCode;
            newEntry.Next = bucket - 1;
            newEntry.Value = value;
            bucket = _count + 1;

            _count++;
            return true;
        }

        [StructLayout(LayoutKind.Auto)]
        private record struct Entry(uint Hash, int Next, T Value);
    }
}