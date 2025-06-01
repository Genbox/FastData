using System.Globalization;
using BenchmarkDotNet.Order;

namespace Genbox.FastData.Benchmarks.Benchmarks;

/// <summary>Benchmark used to illustrate the algorithmic complexity differences between Array and HashSet. Needed for the Readme.</summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ArrayVsHashSetBenchmarks
{
    private readonly string[] _array = Enumerable.Range(0, 1_000_000).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).ToArray();
    private readonly HashSet<string> _hashSet = Enumerable.Range(0, 1_000_000).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).ToHashSet(StringComparer.Ordinal);

    [Benchmark(Baseline = true)]
    public bool Array() => _array.Contains(Random.Shared.Next(0, 1_000_000).ToString(NumberFormatInfo.InvariantInfo));

    [Benchmark]
    public bool HashSet() => _hashSet.Contains(Random.Shared.Next(0, 1_000_000).ToString(NumberFormatInfo.InvariantInfo));
}