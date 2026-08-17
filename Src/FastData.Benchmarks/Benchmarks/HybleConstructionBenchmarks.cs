using BenchmarkDotNet.Order;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class HybleConstructionBenchmarks
{
    private int[] _keys = null!;
    private HybleStructure<int, byte> _structure = null!;

    [Params(32, 256, 1_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keys = new int[Count];

        for (int i = 0; i < _keys.Length; i++)
            _keys[i] = i;

        HashData hashData = HashData.Create(_keys, 1f, static key => SplitMix64.Next((uint)key));
        _structure = new HybleStructure<int, byte>(hashData);

        if (_structure.Create(_keys, ReadOnlyMemory<byte>.Empty) == null)
            throw new InvalidOperationException("Hyble construction failed for the benchmark data set.");
    }

    [Benchmark]
    public HybleContext<int, byte>? Create() => _structure.Create(_keys, ReadOnlyMemory<byte>.Empty);
}