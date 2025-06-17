using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed class Simulator<T>(int length, int capacityFactor = 1) where T : notnull
{
    private readonly int _capacity = length * capacityFactor;
    private readonly NoEqualityEmulator _set = new NoEqualityEmulator((uint)(length * capacityFactor));

    internal Candidate Run(ReadOnlySpan<T> data, IStringHash stringHash, Func<double>? extraFitness = null)
    {
        _set.SetHash(stringHash.GetHashFunction());

        int collisions = 0;
        foreach (T str in data)
        {
            if (!_set.Add((string)(object)str))
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