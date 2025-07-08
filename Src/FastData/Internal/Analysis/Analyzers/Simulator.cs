using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed class Simulator(int length, GeneratorEncoding encoding, int capacityFactor = 1)
{
    private readonly int _capacity = length * capacityFactor;
    private readonly NoEqualityEmulator _set = new NoEqualityEmulator((uint)(length * capacityFactor));
    private readonly Encoding _encoding = encoding == GeneratorEncoding.UTF8 ? Encoding.UTF8 : Encoding.Unicode;

    internal Candidate Run(ReadOnlySpan<string> data, IStringHash stringHash, Func<double>? extraFitness = null)
    {
        _set.SetHash(stringHash.GetExpression().Compile());

        int collisions = 0;
        foreach (string str in data)
        {
            byte[] bytes = _encoding.GetBytes(str);

            if (!_set.Add(bytes))
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
        private StringHashFunc _hashFunc = null!;

        public void SetHash(StringHashFunc hashFunc) => _hashFunc = hashFunc;

        public bool Add(byte[] value)
        {
            ulong hashCode = _hashFunc(value, value.Length);
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