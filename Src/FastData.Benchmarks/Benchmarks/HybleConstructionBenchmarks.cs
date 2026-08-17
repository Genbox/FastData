using BenchmarkDotNet.Order;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal;
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

        HashData hashData = HashData.Create(_keys, 1f, static key => Mix((uint)key));
        _structure = new HybleStructure<int, byte>(hashData);

        if (_structure.Create(_keys, ReadOnlyMemory<byte>.Empty) == null)
            throw new InvalidOperationException("Hyble construction failed for the benchmark data set.");
    }

    [Benchmark]
    public HybleContext<int, byte>? Create() => _structure.Create(_keys, ReadOnlyMemory<byte>.Empty);

    private static ulong Mix(uint value)
    {
        unchecked
        {
            ulong hash = value + 0x9e3779b97f4a7c15UL;
            hash = (hash ^ (hash >> 30)) * 0xbf58476d1ce4e5b9UL;
            hash = (hash ^ (hash >> 27)) * 0x94d049bb133111ebUL;
            return hash ^ (hash >> 31);
        }
    }
}