using BenchmarkDotNet.Order;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PgmConstructionBenchmarks
{
    private int[] _keys = null!;
    private PgmStructure<int, int> _structure = null!;

    [Params(32, 1000, 10000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keys = new int[Count];
        for (int i = 0; i < _keys.Length; i++)
            _keys[i] = i * 3;

        _structure = new PgmStructure<int, int>();
    }

    [Benchmark]
    public object Construct() => _structure.Create(_keys, ReadOnlyMemory<int>.Empty);
}