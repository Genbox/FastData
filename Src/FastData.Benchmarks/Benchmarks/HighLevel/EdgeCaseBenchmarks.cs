using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Benchmarks.Code;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Benchmarks.Benchmarks.HighLevel;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[HideColumns("set")]
public class EdgeCaseBenchmarks
{
    [Benchmark, ArgumentsSource(nameof(HashSetOptimizedModData))]
    public bool HashSet(IFastSet set, string mode) => DoCheck(set);

    [Benchmark, ArgumentsSource(nameof(SingleValueData))]
    public bool SingleValue(IFastSet set, string mode) => DoCheck(set);

    [Benchmark, ArgumentsSource(nameof(UniqueKeyLengthData))]
    public bool UniqueKeyLength(IFastSet set, string mode) => DoCheck(set);

    [Benchmark, ArgumentsSource(nameof(EarlyExitData))]
    public bool EarlyExit(IFastSet set, string mode) => DoCheck(set);

    private static bool DoCheck(IFastSet set)
    {
        bool a = true;

        for (int i = 1; i < 15; i++)
            a &= set.Contains("item" + i);

        return a;
    }

    public IEnumerable<object[]> HashSetOptimizedModData()
    {
        //We return 8 items, which is a power of two, and as such, the mod function will be optimized
        string[] items = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8"];

        yield return [CodeGenerator.DynamicCreateSet<FastDataGenerator>(items, StorageMode.HashSet, true), StorageMode.HashSet];
        yield return [new UnoptimizedHashSet(items), nameof(UnoptimizedHashSet)];
    }

    public IEnumerable<object[]> SingleValueData()
    {
        //We return one item. SingleValue should be faster than an array with one item.
        string[] items = ["item1"];

        yield return [CodeGenerator.DynamicCreateSet<FastDataGenerator>(items, StorageMode.SingleValue, true), StorageMode.SingleValue];
        yield return [new UnoptimizedArray(items), nameof(UnoptimizedArray)];
    }

    public IEnumerable<object[]> UniqueKeyLengthData()
    {
        //Since all the items are unique length, it should be faster to use the length than hashsets or arrays
        string[] items = ["a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa"];

        yield return [CodeGenerator.DynamicCreateSet<FastDataGenerator>(items, StorageMode.UniqueKeyLength, true), StorageMode.UniqueKeyLength];
        yield return [new UnoptimizedArray(items), nameof(UnoptimizedArray)];
        yield return [new UnoptimizedHashSet(items), nameof(UnoptimizedHashSet)];
    }

    public IEnumerable<object[]> EarlyExitData()
    {
        //We add 9 items, but they all have the same length. The optimized array should early exit on length.
        string[] items = ["item", "item10", "item11", "item12", "item13", "item14", "item15", "item16", "item17", "item18"];

        yield return [CodeGenerator.DynamicCreateSet<FastDataGenerator>(items, StorageMode.Array, true), StorageMode.Array];
        yield return [new UnoptimizedArray(items), nameof(UnoptimizedArray)];
    }
}