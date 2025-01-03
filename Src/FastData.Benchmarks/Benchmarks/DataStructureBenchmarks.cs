using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData;
using Genbox.FastData.Enums;
using Genbox.FastFilter;

[assembly: FastData<string>("StaticLinear", ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "100"], StorageMode = StorageMode.Linear)]
[assembly: FastData<string>("StaticLogic", ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "100"], StorageMode = StorageMode.Logic)]
[assembly: FastData<string>("StaticTree", ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "100"], StorageMode = StorageMode.Tree)]
[assembly: FastData<string>("StaticIndex", ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "100"], StorageMode = StorageMode.Indexed)]

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser(false)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class DataStructureBenchmarks
{
    private string[] _queries = null!;
    private string[] _array = null!;

    private HashSet<string> _hashSet = null!;
    private Dictionary<string, int> _dictionary = null!;
    private FrozenDictionary<string, int> _frozenDictionary = null!;
    private ConcurrentDictionary<string, int> _concurrentDictionary = null!;
    private BloomFilter<string> _bloom = null!;
    private BlockedBloomFilter<string> _blockedBloom = null!;
    private BinaryFuse8Filter<string> _binaryFuse8 = null!;

    [Params(100)]
    public int DataSize { get; set; }

    [Params(1_000)]
    public int QuerySize { get; set; }

    private void SetupQueries()
    {
        _queries = new string[QuerySize];

        //Half the queries are within the set
        int i;
        for (i = 0; i < QuerySize / 2; i++)
            _queries[i] = Random.Shared.Next(1, DataSize + 1).ToString(CultureInfo.InvariantCulture);

        //Half the queries are outside the set
        for (; i < QuerySize; i++)
            _queries[i] = Random.Shared.Next(DataSize + 1, int.MaxValue).ToString(NumberFormatInfo.InvariantInfo);
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

    [GlobalSetup(Target = nameof(QueryDictionary))]
    public void SetupDictionary()
    {
        SetupQueries();
        _dictionary = CreateDictionary();
    }

    [GlobalSetup(Target = nameof(QueryFrozenDictionary))]
    public void SetupFrozenDictionary()
    {
        SetupQueries();
        _frozenDictionary = CreateFrozenDictionary();
    }

    [GlobalSetup(Target = nameof(QueryConcurrentDictionary))]
    public void SetupConcurrentDictionary()
    {
        SetupQueries();
        _concurrentDictionary = CreateConcurrentDictionary();
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
    public void ConstructDictionary() => CreateDictionary();

    [Benchmark, BenchmarkCategory("Construction")]
    public void ConstructFrozenDictionary() => CreateFrozenDictionary();

    [Benchmark, BenchmarkCategory("Construction")]
    public void ConstructConcurrentDictionary() => CreateConcurrentDictionary();

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
    public void QueryDictionary() => DoQuery(_dictionary.ContainsKey);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryFrozenDictionary() => DoQuery(_frozenDictionary.ContainsKey);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryConcurrentDictionary() => DoQuery(_concurrentDictionary.ContainsKey);

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

        for (int i = 0; i < arr.Length; i++)
            arr[i] = i.ToString(NumberFormatInfo.InvariantInfo);

        return arr;
    }

    private HashSet<string> CreateHashSet() => new HashSet<string>(CreateArray());
    private Dictionary<string, int> CreateDictionary() => new Dictionary<string, int>(CreateArray().Select(x => new KeyValuePair<string, int>(x, 42)));
    private FrozenDictionary<string, int> CreateFrozenDictionary() => CreateDictionary().ToFrozenDictionary();
    private ConcurrentDictionary<string, int> CreateConcurrentDictionary() => new ConcurrentDictionary<string, int>(CreateArray().Select(x => new KeyValuePair<string, int>(x, 42)));
    private BloomFilter<string> CreateBloomFilter() => new BloomFilter<string>(CreateArray());
    private BlockedBloomFilter<string> CreateBlockedBloomFilter() => new BlockedBloomFilter<string>(CreateArray());
    private BinaryFuse8Filter<string> CreateBinaryFuse8Filter() => new BinaryFuse8Filter<string>(CreateArray());

    private void DoQuery(Func<string, bool> query)
    {
        for (int i = 0; i < _queries.Length; i++)
            query(_queries[i]);
    }
}