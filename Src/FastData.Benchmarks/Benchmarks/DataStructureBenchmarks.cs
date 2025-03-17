using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.InternalShared;
using Genbox.FastFilter;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser(false)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class DataStructureBenchmarks
{
    private readonly string[] _data = ["a", "aa", "bbb", "cccc", "aaaaa", "cccccc", "ddddddd", "00000000", "uuuuuuuuu", "aaaaaaaaaa"];
    private string[] _queries = null!;

    private string[] _array = null!;
    private HashSet<string> _hashSet = null!;
    private FrozenSet<string> _frozenSet = null!;
    private BloomFilter<string> _bloom = null!;
    private BlockedBloomFilter<string> _blockedBloom = null!;
    private BinaryFuse8Filter<string> _binaryFuse8 = null!;

    private IFastSet<string> _fastArray = null!;
    private IFastSet<string> _fastBinarySearch = null!;
    private IFastSet<string> _fastEytzingerSearch = null!;
    private IFastSet<string> _fastSwitch = null!;
    private IFastSet<string> _fastMinimalPerfectHash = null!;
    private IFastSet<string> _fastHashSetLinear = null!;
    private IFastSet<string> _fastHashSetChain = null!;
    private IFastSet<string> _fastUniqueKeyLength = null!;
    private IFastSet<string> _fastUniqueKeyLengthSwitch = null!;
    private IFastSet<string> _fastKeyLength = null!;

    // private IFastSet<string> _fastSingleValue = null!;
    private IFastSet<string> _fastConditional = null!;

    [Params(1_000, 10_000)]
    public int QuerySize { get; set; }

    private IFastSet<string> CreateFastData(DataStructure ds)
    {
        FastDataConfig config = new FastDataConfig(ds.ToString(), _data.Cast<object>().ToArray()); //We have to convert to object[]

        string code = FastDataGenerator.Generate(ds, config, new CSharpCodeGenerator(new CSharpGeneratorConfig
        {
            ClassType = ClassType.Instance
        }));

        return CodeGenerator.CreateFastSet<string>(code, true);
    }

    private void SetupQueries()
    {
        _queries = new string[QuerySize];

        //Half the queries are within the set
        int i;
        for (i = 0; i < QuerySize / 2; i++)
            _queries[i] = _data[Random.Shared.Next(1, _data.Length)];

        //Half the queries are outside the set
        for (; i < QuerySize; i++)
            _queries[i] = "item" + Random.Shared.Next(_data.Length + 1, int.MaxValue).ToString(NumberFormatInfo.InvariantInfo);
    }

    [GlobalSetup(Target = nameof(QueryArray))]
    public void SetupArray()
    {
        SetupQueries();
        _array = _data;
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

    [GlobalSetup(Target = nameof(QueryStaticArray))]
    public void SetupStaticArray()
    {
        SetupQueries();
        _fastArray = CreateFastData(DataStructure.Array);
    }

    [GlobalSetup(Target = nameof(QueryStaticBinarySearch))]
    public void SetupStaticBinarySearch()
    {
        SetupQueries();
        _fastBinarySearch = CreateFastData(DataStructure.BinarySearch);
    }

    [GlobalSetup(Target = nameof(QueryStaticEytzingerSearch))]
    public void SetupStaticEytzingerSearch()
    {
        SetupQueries();
        _fastEytzingerSearch = CreateFastData(DataStructure.EytzingerSearch);
    }

    [GlobalSetup(Target = nameof(QueryStaticSwitch))]
    public void SetupStaticSwitch()
    {
        SetupQueries();
        _fastSwitch = CreateFastData(DataStructure.Switch);
    }

    [GlobalSetup(Target = nameof(QueryStaticMinimalPerfectHash))]
    public void SetupStaticMinimalPerfectHash()
    {
        SetupQueries();
        _fastMinimalPerfectHash = CreateFastData(DataStructure.MinimalPerfectHash);
    }

    [GlobalSetup(Target = nameof(QueryStaticHashSetLinear))]
    public void SetupStaticHashSetLinear()
    {
        SetupQueries();
        _fastHashSetLinear = CreateFastData(DataStructure.HashSetLinear);
    }

    [GlobalSetup(Target = nameof(QueryStaticHashSetChain))]
    public void SetupStaticHashSetChain()
    {
        SetupQueries();
        _fastHashSetChain = CreateFastData(DataStructure.HashSetChain);
    }

    [GlobalSetup(Target = nameof(QueryStaticUniqueKeyLength))]
    public void SetupStaticUniqueKeyLength()
    {
        SetupQueries();
        _fastUniqueKeyLength = CreateFastData(DataStructure.UniqueKeyLength);
    }

    [GlobalSetup(Target = nameof(QueryStaticUniqueKeyLengthSwitch))]
    public void SetupStaticUniqueKeyLengthSwitch()
    {
        SetupQueries();
        _fastUniqueKeyLengthSwitch = CreateFastData(DataStructure.UniqueKeyLengthSwitch);
    }

    [GlobalSetup(Target = nameof(QueryStaticKeyLength))]
    public void SetupStaticKeyLength()
    {
        SetupQueries();
        _fastKeyLength = CreateFastData(DataStructure.KeyLength);
    }

    [GlobalSetup(Target = nameof(QueryStaticConditional))]
    public void SetupStaticConditional()
    {
        SetupQueries();
        _fastConditional = CreateFastData(DataStructure.Conditional);
    }

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
    public void QueryStaticArray() => DoQuery(_fastArray.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticBinarySearch() => DoQuery(_fastBinarySearch.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticEytzingerSearch() => DoQuery(_fastEytzingerSearch.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticSwitch() => DoQuery(_fastSwitch.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticMinimalPerfectHash() => DoQuery(_fastMinimalPerfectHash.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticHashSetLinear() => DoQuery(_fastHashSetLinear.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticHashSetChain() => DoQuery(_fastHashSetChain.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticUniqueKeyLength() => DoQuery(_fastUniqueKeyLength.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticUniqueKeyLengthSwitch() => DoQuery(_fastUniqueKeyLengthSwitch.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticKeyLength() => DoQuery(_fastKeyLength.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticConditional() => DoQuery(_fastConditional.Contains);

    private HashSet<string> CreateHashSet() => new HashSet<string>(_data, StringComparer.Ordinal);
    private FrozenSet<string> CreateFrozenSet() => CreateHashSet().ToFrozenSet();
    private BloomFilter<string> CreateBloomFilter() => new BloomFilter<string>(_data);
    private BlockedBloomFilter<string> CreateBlockedBloomFilter() => new BlockedBloomFilter<string>(_data);
    private BinaryFuse8Filter<string> CreateBinaryFuse8Filter() => new BinaryFuse8Filter<string>(_data);

    private void DoQuery(Func<string, bool> query)
    {
        for (int i = 0; i < _queries.Length; i++)
            query(_queries[i]);
    }
}