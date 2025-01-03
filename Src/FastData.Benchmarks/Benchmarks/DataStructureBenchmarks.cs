using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData;
using Genbox.FastData.Enums;
using Genbox.FastFilter;

[assembly: FastData<string>("StaticAuto", ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "100"], StorageMode = StorageMode.Auto)]
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
    private string[] _binarySearch = null!;
    private string[] _eytzinger = null!;

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

    [GlobalSetup(Target = nameof(QueryEytzingerSearch))]
    public void SetupEytzinger()
    {
        SetupQueries();
        _eytzinger = CreateEytzinger();
    }

    [GlobalSetup(Target = nameof(QueryBinarySearch))]
    public void SetupBinarySearch()
    {
        SetupQueries();
        _binarySearch = CreateBinarySearch();
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

    [GlobalSetup(Targets = [nameof(QuerySwitch), nameof(QueryIfElse), nameof(QueryStaticLinear), nameof(QueryStaticLogic), nameof(QueryStaticTree), nameof(QueryStaticIndex)])]
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
    public void QueryBinarySearch() => DoQuery(BinarySearch);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryEytzingerSearch() => DoQuery(EytzingerSearch);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryHashSet() => DoQuery(_hashSet.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryDictionary() => DoQuery(_dictionary.ContainsKey);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryFrozenDictionary() => DoQuery(_frozenDictionary.ContainsKey);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryConcurrentDictionary() => DoQuery(_concurrentDictionary.ContainsKey);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryIfElse() => DoQuery(IfElse);

    [Benchmark, BenchmarkCategory("Query")]
    public void QuerySwitch() => DoQuery(Switch);

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

    private string[] CreateEytzinger()
    {
        string[] data = CreateArray();
        Array.Sort(data);

        string[] eytzinger = new string[DataSize];

        int idx = 0;
        EytzingerOrder(data, eytzinger, ref idx);
        return eytzinger;
    }

    private string[] CreateBinarySearch()
    {
        string[] data = CreateArray();
        Array.Sort(data);
        return data;
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

    private static bool Switch(string value)
    {
        switch (value)
        {
            case "1":
            case "2":
            case "3":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
            case "13":
            case "14":
            case "15":
            case "16":
            case "17":
            case "18":
            case "19":
            case "20":
            case "21":
            case "22":
            case "23":
            case "24":
            case "25":
            case "26":
            case "27":
            case "28":
            case "29":
            case "30":
            case "31":
            case "32":
            case "33":
            case "34":
            case "35":
            case "36":
            case "37":
            case "38":
            case "39":
            case "40":
            case "41":
            case "42":
            case "43":
            case "44":
            case "45":
            case "46":
            case "47":
            case "48":
            case "49":
            case "50":
            case "51":
            case "52":
            case "53":
            case "54":
            case "55":
            case "56":
            case "57":
            case "58":
            case "59":
            case "60":
            case "61":
            case "62":
            case "63":
            case "64":
            case "65":
            case "66":
            case "67":
            case "68":
            case "69":
            case "70":
            case "71":
            case "72":
            case "73":
            case "74":
            case "75":
            case "76":
            case "77":
            case "78":
            case "79":
            case "80":
            case "81":
            case "82":
            case "83":
            case "84":
            case "85":
            case "86":
            case "87":
            case "88":
            case "89":
            case "90":
            case "91":
            case "92":
            case "93":
            case "94":
            case "95":
            case "96":
            case "97":
            case "98":
            case "99":
            case "100":
                return true;
            default:
                return false;
        }
    }

    private static bool IfElse(string value)
    {
        if (value == "1") return true;
        if (value == "2") return true;
        if (value == "3") return true;
        if (value == "4") return true;
        if (value == "5") return true;
        if (value == "6") return true;
        if (value == "7") return true;
        if (value == "8") return true;
        if (value == "9") return true;
        if (value == "10") return true;
        if (value == "11") return true;
        if (value == "12") return true;
        if (value == "13") return true;
        if (value == "14") return true;
        if (value == "15") return true;
        if (value == "16") return true;
        if (value == "17") return true;
        if (value == "18") return true;
        if (value == "19") return true;
        if (value == "20") return true;
        if (value == "21") return true;
        if (value == "22") return true;
        if (value == "23") return true;
        if (value == "24") return true;
        if (value == "25") return true;
        if (value == "26") return true;
        if (value == "27") return true;
        if (value == "28") return true;
        if (value == "29") return true;
        if (value == "30") return true;
        if (value == "31") return true;
        if (value == "32") return true;
        if (value == "33") return true;
        if (value == "34") return true;
        if (value == "35") return true;
        if (value == "36") return true;
        if (value == "37") return true;
        if (value == "38") return true;
        if (value == "39") return true;
        if (value == "40") return true;
        if (value == "41") return true;
        if (value == "42") return true;
        if (value == "43") return true;
        if (value == "44") return true;
        if (value == "45") return true;
        if (value == "46") return true;
        if (value == "47") return true;
        if (value == "48") return true;
        if (value == "49") return true;
        if (value == "50") return true;
        if (value == "51") return true;
        if (value == "52") return true;
        if (value == "53") return true;
        if (value == "54") return true;
        if (value == "55") return true;
        if (value == "56") return true;
        if (value == "57") return true;
        if (value == "58") return true;
        if (value == "59") return true;
        if (value == "60") return true;
        if (value == "61") return true;
        if (value == "62") return true;
        if (value == "63") return true;
        if (value == "64") return true;
        if (value == "65") return true;
        if (value == "66") return true;
        if (value == "67") return true;
        if (value == "68") return true;
        if (value == "69") return true;
        if (value == "70") return true;
        if (value == "71") return true;
        if (value == "72") return true;
        if (value == "73") return true;
        if (value == "74") return true;
        if (value == "75") return true;
        if (value == "76") return true;
        if (value == "77") return true;
        if (value == "78") return true;
        if (value == "79") return true;
        if (value == "80") return true;
        if (value == "81") return true;
        if (value == "82") return true;
        if (value == "83") return true;
        if (value == "84") return true;
        if (value == "85") return true;
        if (value == "86") return true;
        if (value == "87") return true;
        if (value == "88") return true;
        if (value == "89") return true;
        if (value == "90") return true;
        if (value == "91") return true;
        if (value == "92") return true;
        if (value == "93") return true;
        if (value == "94") return true;
        if (value == "95") return true;
        if (value == "96") return true;
        if (value == "97") return true;
        if (value == "98") return true;
        if (value == "99") return true;
        if (value == "100") return true;
        return false;
    }

    private bool BinarySearch(string value)
    {
        int lo = 0;
        int hi = _binarySearch.Length - 1;
        while (lo <= hi)
        {
            int i = lo + ((hi - lo) >> 1);
            int order = string.CompareOrdinal(_binarySearch[i], value);

            if (order == 0)
                return true;

            if (order < 0)
                lo = i + 1;
            else
                hi = i - 1;
        }

        return false;
    }

    private static void EytzingerOrder<T>(T[] input, T[] output, ref int arrIdx, int eytIdx = 0)
    {
        if (eytIdx < input.Length)
        {
            EytzingerOrder(input, output, ref arrIdx, (2 * eytIdx) + 1);
            output[eytIdx] = input[arrIdx++];
            EytzingerOrder(input, output, ref arrIdx, (2 * eytIdx) + 2);
        }
    }

    private bool EytzingerSearch(string value)
    {
        int i = 0;
        while (i < (uint)_eytzinger.Length)
        {
            int comparison = string.CompareOrdinal(_eytzinger[i], value);

            if (comparison == 0)
                return true;

            if (comparison < 0)
                i = 2 * i + 2;
            else
                i = 2 * i + 1;
        }

        return false;
    }
}