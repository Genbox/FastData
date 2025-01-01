using BenchmarkDotNet.Configs;
using Genbox.FastData.InternalShared.Optimal;

namespace Genbox.FastData.Benchmarks.Benchmarks.DataStructures;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
public class OptimalStructureBenchmarks
{
    private static readonly string[] _array = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];
    private static readonly string[] _arraySorted = ["item1", "item10", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9"];
    private static readonly HashSet<string> _hashSet = new HashSet<string>(["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"], StringComparer.Ordinal);

    [Params("item1", "item5", "item10")]
    public string Item { get; set; }

    [Benchmark(Baseline = true), BenchmarkCategory("Linear")]
    public bool ArrayTest() => _array.Contains(Item, StringComparer.Ordinal);

    [Benchmark, BenchmarkCategory("Linear")]
    public bool OptimalArrayTest() => OptimalArray.Contains(Item);

    [Benchmark, BenchmarkCategory("Linear")]
    public bool OptimalConditionalTest() => OptimalConditional.Contains(Item);

    [Benchmark, BenchmarkCategory("Linear")]
    public bool OptimalSwitchTest() => OptimalSwitch.Contains(Item);

    [Benchmark(Baseline = true), BenchmarkCategory("Logarithmic")]
    public int BinarySearchTest() => Array.BinarySearch(_arraySorted, Item, StringComparer.Ordinal);

    [Benchmark, BenchmarkCategory("Logarithmic")]
    public bool OptimalBinarySearchTest() => OptimalBinarySearch.Contains(Item);

    [Benchmark, BenchmarkCategory("Logarithmic")]
    public bool OptimalEytzingerSearchTest() => OptimalEytzingerSearch.Contains(Item);

    [Benchmark(Baseline = true), BenchmarkCategory("Constant")]
    public bool HashSetTest() => _hashSet.Contains(Item);

    [Benchmark, BenchmarkCategory("Constant")]
    public bool OptimalHashSetTest() => OptimalHashSet.Contains(Item);

    [Benchmark, BenchmarkCategory("Constant")]
    public bool OptimalKeyLengthTest() => OptimalKeyLength.Contains(Item);

    [Benchmark, BenchmarkCategory("Constant")]
    public bool OptimalMinimalPerfectHashTest() => OptimalMinimalPerfectHash.Contains(Item);

    [Benchmark, BenchmarkCategory("Constant")]
    public bool OptimalSingleValueTest() => OptimalSingleValue.Contains(Item);

    [Benchmark, BenchmarkCategory("Constant")]
    public bool OptimalSwitchHashCodeTest() => OptimalSwitchHashCode.Contains(Item);

    [Benchmark, BenchmarkCategory("Constant")]
    public bool OptimalUniqueKeyLengthTest() => OptimalUniqueKeyLength.Contains(Item);

    [Benchmark, BenchmarkCategory("Constant")]
    public bool OptimalUniqueKeyLengthSwitchTest() => OptimalUniqueKeyLengthSwitch.Contains(Item);
}