using System.Linq.Expressions;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed class Simulator
{
    private readonly int _capacity;
    private readonly byte[][] _data;
    private readonly NoEqualityEmulator _set;

    internal Simulator(ReadOnlySpan<string> data, GeneratorEncoding encoding, int capacityFactor = 1)
    {
        _capacity = data.Length * capacityFactor;
        _set = new NoEqualityEmulator((uint)_capacity);
        UnitSize = StringHelper.GetSize(encoding);

        Func<string, byte[]> getBytes = StringHelper.GetBytesFunc(encoding);
        _data = new byte[data.Length][];

        for (int i = 0; i < data.Length; i++)
            _data[i] = getBytes(data[i]);
    }

    internal int UnitSize { get; }
    internal byte[][] EncodedData => _data;

    internal Candidate Run(IStringHash stringHash, Func<Expression, double>? extraFitness = null)
    {
        Expression<StringHashFunc> expression = stringHash.GetExpression();
        _set.SetHash(expression.Compile());

        int collisions = 0;
        foreach (byte[] bytes in _data)
        {
            if (!_set.Add(bytes))
                collisions++;
        }

        _set.Clear();

        double fitness = (_capacity - collisions) / (double)_capacity;

        if (extraFitness != null)
            fitness = (fitness + extraFitness(expression)) * 0.5;

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