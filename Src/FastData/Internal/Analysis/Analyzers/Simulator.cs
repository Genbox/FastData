using Genbox.FastData.Generators;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed class Simulator(int capacityFactor = 1)
{
    private NoEqualityEmulator? _set;

    internal Candidate Run(ReadOnlySpan<string> data, IStringHash stringHash, Func<double>? extraFitness = null)
    {
        uint _capacity = (uint)(data.Length * capacityFactor);
        _set ??= new NoEqualityEmulator(_capacity);
        _set.SetHash(stringHash.GetHashFunction());

        int collisions = 0;
        foreach (string str in data)
        {
            if (!_set.Add(str))
                collisions++;
        }

        _set.Clear();

        double fitness = (_capacity - collisions) / (double)_capacity;

        if (extraFitness != null)
            fitness = (fitness + extraFitness()) * 0.5;

        return new Candidate(stringHash, fitness, collisions);
    }

    private sealed class NoEqualityEmulator(uint capacity)
    {
        private readonly int[] _buckets = new int[capacity];
        private HashFunc<string> _hashFunc = null!;

        public void SetHash(HashFunc<string> hashFunc) => _hashFunc = hashFunc;

        public bool Add(string value)
        {
            ulong hashCode = _hashFunc(value);
            ref int bucket = ref _buckets[hashCode % (ulong)_buckets.Length];

            if (bucket == 0)
            {
                bucket++;
                return true;
            }

            bucket++;
            return false;
        }

        public void Clear() => Array.Clear(_buckets, 0, _buckets.Length);
    }
}