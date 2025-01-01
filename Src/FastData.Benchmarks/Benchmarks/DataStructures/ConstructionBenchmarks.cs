using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Genbox.FastData.Benchmarks.Benchmarks.DataStructures;

[MemoryDiagnoser(false)]
public class ConstructionBenchmarks
{
    [Params(1_000_000)]
    public int Size { get; set; }

    [Benchmark]
    public int[] ConstructArray() => Enumerable.Range(0, Size).ToArray();

    [Benchmark]
    public HashSet<int> ConstructHashSet() => new HashSet<int>(Enumerable.Range(0, Size));

    [Benchmark]
    public Dictionary<int, int> ConstructDictionary() => new Dictionary<int, int>(Enumerable.Range(0, Size).Select(x => new KeyValuePair<int, int>(x, x)));

    [Benchmark]
    public FrozenDictionary<int, int> ConstructFrozenDictionary() => new Dictionary<int, int>(Enumerable.Range(0, Size).Select(x => new KeyValuePair<int, int>(x, x))).ToFrozenDictionary();

    [Benchmark]
    public ConcurrentDictionary<int, int> ConstructConcurrentDictionary() => new ConcurrentDictionary<int, int>(Enumerable.Range(0, Size).Select(x => new KeyValuePair<int, int>(x, x)));
}