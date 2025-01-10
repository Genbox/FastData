using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData;
using Genbox.FastData.Enums;
using Genbox.FastFilter;

[assembly: FastData<string>("StaticLinear", ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10", "item11", "item12", "item13", "item14", "item15", "item16", "item17", "item18", "item19", "item20", "item21", "item22", "item23", "item24", "item25", "item26", "item27", "item28", "item29", "item30", "item31", "item32", "item33", "item34", "item35", "item36", "item37", "item38", "item39", "item40", "item41", "item42", "item43", "item44", "item45", "item46", "item47", "item48", "item49", "item50", "item51", "item52", "item53", "item54", "item55", "item56", "item57", "item58", "item59", "item60", "item61", "item62", "item63", "item64", "item65", "item66", "item67", "item68", "item69", "item70", "item71", "item72", "item73", "item74", "item75", "item76", "item77", "item78", "item79", "item80", "item81", "item82", "item83", "item84", "item85", "item86", "item87", "item88", "item89", "item90", "item91", "item92", "item93", "item94", "item95", "item96", "item97", "item98", "item99", "item100"], StorageMode = StorageMode.Linear)]
[assembly: FastData<string>("StaticLogic", ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10", "item11", "item12", "item13", "item14", "item15", "item16", "item17", "item18", "item19", "item20", "item21", "item22", "item23", "item24", "item25", "item26", "item27", "item28", "item29", "item30", "item31", "item32", "item33", "item34", "item35", "item36", "item37", "item38", "item39", "item40", "item41", "item42", "item43", "item44", "item45", "item46", "item47", "item48", "item49", "item50", "item51", "item52", "item53", "item54", "item55", "item56", "item57", "item58", "item59", "item60", "item61", "item62", "item63", "item64", "item65", "item66", "item67", "item68", "item69", "item70", "item71", "item72", "item73", "item74", "item75", "item76", "item77", "item78", "item79", "item80", "item81", "item82", "item83", "item84", "item85", "item86", "item87", "item88", "item89", "item90", "item91", "item92", "item93", "item94", "item95", "item96", "item97", "item98", "item99", "item100"], StorageMode = StorageMode.Logic)]
[assembly: FastData<string>("StaticTree", ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10", "item11", "item12", "item13", "item14", "item15", "item16", "item17", "item18", "item19", "item20", "item21", "item22", "item23", "item24", "item25", "item26", "item27", "item28", "item29", "item30", "item31", "item32", "item33", "item34", "item35", "item36", "item37", "item38", "item39", "item40", "item41", "item42", "item43", "item44", "item45", "item46", "item47", "item48", "item49", "item50", "item51", "item52", "item53", "item54", "item55", "item56", "item57", "item58", "item59", "item60", "item61", "item62", "item63", "item64", "item65", "item66", "item67", "item68", "item69", "item70", "item71", "item72", "item73", "item74", "item75", "item76", "item77", "item78", "item79", "item80", "item81", "item82", "item83", "item84", "item85", "item86", "item87", "item88", "item89", "item90", "item91", "item92", "item93", "item94", "item95", "item96", "item97", "item98", "item99", "item100"], StorageMode = StorageMode.Tree)]
[assembly: FastData<string>("StaticIndex", ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10", "item11", "item12", "item13", "item14", "item15", "item16", "item17", "item18", "item19", "item20", "item21", "item22", "item23", "item24", "item25", "item26", "item27", "item28", "item29", "item30", "item31", "item32", "item33", "item34", "item35", "item36", "item37", "item38", "item39", "item40", "item41", "item42", "item43", "item44", "item45", "item46", "item47", "item48", "item49", "item50", "item51", "item52", "item53", "item54", "item55", "item56", "item57", "item58", "item59", "item60", "item61", "item62", "item63", "item64", "item65", "item66", "item67", "item68", "item69", "item70", "item71", "item72", "item73", "item74", "item75", "item76", "item77", "item78", "item79", "item80", "item81", "item82", "item83", "item84", "item85", "item86", "item87", "item88", "item89", "item90", "item91", "item92", "item93", "item94", "item95", "item96", "item97", "item98", "item99", "item100"], StorageMode = StorageMode.Indexed)]

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser(false)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class DataStructureBenchmarks
{
    private const int DataSize = 100;

    private string[] _queries = null!;
    private string[] _array = null!;

    private HashSet<string> _hashSet = null!;
    private FrozenSet<string> _frozenSet = null!;
    private BloomFilter<string> _bloom = null!;
    private BlockedBloomFilter<string> _blockedBloom = null!;
    private BinaryFuse8Filter<string> _binaryFuse8 = null!;

    [Params(1_000, 10_000, 100_000)]
    public int QuerySize { get; set; }

    private void SetupQueries()
    {
        _queries = new string[QuerySize];

        //Half the queries are within the set
        int i;
        for (i = 0; i < QuerySize / 2; i++)
            _queries[i] = "item" + Random.Shared.Next(1, DataSize + 1).ToString(NumberFormatInfo.InvariantInfo);

        //Half the queries are outside the set
        for (; i < QuerySize; i++)
            _queries[i] = "item" + Random.Shared.Next(DataSize + 1, int.MaxValue).ToString(NumberFormatInfo.InvariantInfo);
    }

    [GlobalSetup(Target = nameof(QueryArray))]
    public void SetupArray()
    {
        SetupQueries();
        _array = CreateArray();
    }

    [GlobalSetup(Target = nameof(QueryHashSet))]
    public void SetupHashSet()
    {
        SetupQueries();
        _hashSet = CreateHashSet();
    }

    [GlobalSetup(Target = nameof(QueryFrozenSet))]
    public void SetupFrozenSet()
    {
        SetupQueries();
        _frozenSet = CreateFrozenSet();
    }

    [GlobalSetup(Target = nameof(QueryBloom))]
    public void SetupBloomFilter()
    {
        SetupQueries();
        _bloom = CreateBloomFilter();
    }

    [GlobalSetup(Target = nameof(QueryBlockBloom))]
    public void SetupBlockedBloomFilter()
    {
        SetupQueries();
        _blockedBloom = CreateBlockedBloomFilter();
    }

    [GlobalSetup(Target = nameof(QueryBinaryFuse8))]
    public void SetupBinaryFuse8Filter()
    {
        SetupQueries();
        _binaryFuse8 = CreateBinaryFuse8Filter();
    }

    [GlobalSetup(Targets = [nameof(QueryStaticLinear), nameof(QueryStaticLogic), nameof(QueryStaticTree), nameof(QueryStaticIndex)])]
    public void SetupOthers() => SetupQueries();

    [Benchmark, BenchmarkCategory("Construction")]
    public void ConstructArray() => CreateArray();

    [Benchmark, BenchmarkCategory("Construction")]
    public void ConstructHashSet() => CreateHashSet();

    [Benchmark, BenchmarkCategory("Construction")]
    public void ConstructBloom() => CreateBloomFilter();

    [Benchmark, BenchmarkCategory("Construction")]
    public void ConstructBlockedBloom() => CreateBlockedBloomFilter();

    [Benchmark, BenchmarkCategory("Construction")]
    public void ConstructBinaryFuse8() => CreateBinaryFuse8Filter();

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryArray() => DoQuery(_array.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryHashSet() => DoQuery(_hashSet.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryFrozenSet() => DoQuery(_frozenSet.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryBloom() => DoQuery(_bloom.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryBlockBloom() => DoQuery(_blockedBloom.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryBinaryFuse8() => DoQuery(_binaryFuse8.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticLinear() => DoQuery(StaticLinear.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticLogic() => DoQuery(StaticLogic.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticTree() => DoQuery(StaticTree.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticIndex() => DoQuery(StaticIndex.Contains);

    private string[] CreateArray()
    {
        string[] arr = new string[DataSize];

        for (uint i = 0; i < arr.Length; i++)
            arr[i] = "item" + (i + 1);

        return arr;
    }

    private HashSet<string> CreateHashSet() => new HashSet<string>(CreateArray(), StringComparer.Ordinal);
    private FrozenSet<string> CreateFrozenSet() => CreateHashSet().ToFrozenSet();
    private BloomFilter<string> CreateBloomFilter() => new BloomFilter<string>(CreateArray());
    private BlockedBloomFilter<string> CreateBlockedBloomFilter() => new BlockedBloomFilter<string>(CreateArray());
    private BinaryFuse8Filter<string> CreateBinaryFuse8Filter() => new BinaryFuse8Filter<string>(CreateArray());

    private void DoQuery(Func<string, bool> query)
    {
        for (int i = 0; i < _queries.Length; i++)
            query(_queries[i]);
    }
}