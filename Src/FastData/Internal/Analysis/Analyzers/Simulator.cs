using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal class Simulator(SimulatorConfig config)
{
    internal void RunWithEqual<T>(string[] data, Candidate<T> cand, EqualFunc<string>? equalFunc = null) where T : IArrayHash
    {
        equalFunc ??= DefaultEqual;

        // Generate a hash function from the spec
        HashFunc hashFunc = cand.Spec.GetHashFunction();
        uint capacity = (uint)(data.Length * config.CapacityFactor);

        cand.Fitness = Emulate(cand.Metadata, data, capacity, hashFunc, equalFunc);
    }

    internal void Run<T>(string[] data, Candidate<T> cand) where T : IArrayHash
    {
        // Generate a hash function from the spec
        HashFunc hashFunc = cand.Spec.GetHashFunction();
        uint capacity = (uint)(data.Length * config.CapacityFactor);

        cand.Fitness = Emulate2(cand.Metadata, data, capacity, hashFunc);
    }

    private static bool DefaultEqual(string a, string b) => a.Equals(b, StringComparison.Ordinal);

    private double Emulate(Dictionary<string, object> metadata, string[] data, uint capacity, HashFunc hashFunc, EqualFunc<string> equalFunc)
    {
        FastSet set = new FastSet(capacity, hashFunc, equalFunc, config.UseUtf8);

        int collisions = 0;
        foreach (string str in data)
        {
            if (!set.Add(str))
                collisions++;
        }

        metadata["Collisions"] = collisions;

        // metadata["MinVariance"] = set.MinVariance;
        // metadata["MaxVariance"] = set.MaxVariance;

        double raw = (double)capacity / (collisions + 1);
        double min = (double)capacity / (data.Length + 1); // worst case: every insertion collides
        double max = capacity; // best case: zero collisions
        return (raw - min) / (max - min);
    }

    private double Emulate2(Dictionary<string, object> metadata, string[] data, uint capacity, HashFunc hashFunc)
    {
        FastSet2 set = new FastSet2(capacity, hashFunc, config.UseUtf8);

        int collisions = 0;
        foreach (string str in data)
        {
            if (!set.Add(str))
                collisions++;
        }

        metadata["Collisions"] = collisions;

        // metadata["MinVariance"] = set.MinVariance;
        // metadata["MaxVariance"] = set.MaxVariance;

        double raw = (double)capacity / (collisions + 1);
        double min = (double)capacity / (data.Length + 1); // worst case: every insertion collides
        double max = capacity; // best case: zero collisions
        return (raw - min) / (max - min);
    }

    private ref struct FastSet(uint capacity, HashFunc hashFunc, EqualFunc<string> equalFunc, bool utf8)
    {
        private readonly int[] _buckets = new int[capacity];
        private readonly Entry[] _entries = new Entry[capacity];
        private int _count;

        //TODO: optimize
        // public readonly int MinVariance => _buckets.Where(x => x != 0).Min();
        // public readonly int MaxVariance => _buckets.Max();

        public bool Add(string value)
        {
            byte[] bytes = utf8 ? Encoding.UTF8.GetBytes(value) : Encoding.Unicode.GetBytes(value);

            ulong hashCode = hashFunc(ref bytes[0], bytes.Length);
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
        private record struct Entry(ulong Hash, int Next, string Value);
    }

    private ref struct FastSet2(uint capacity, HashFunc hashFunc, bool utf8)
    {
        private readonly int[] _buckets = new int[capacity];

        public readonly bool Add(string value)
        {
            //TODO: Can we optimize to avoid re-converting on each call?
            byte[] bytes = utf8 ? Encoding.UTF8.GetBytes(value) : Encoding.Unicode.GetBytes(value);
            ulong hashCode = hashFunc(ref bytes[0], bytes.Length);
            ref int bucket = ref _buckets[hashCode % capacity];

            if (bucket == 0)
            {
                bucket++;
                return true;
            }

            bucket++;
            return false;
        }
    }
}