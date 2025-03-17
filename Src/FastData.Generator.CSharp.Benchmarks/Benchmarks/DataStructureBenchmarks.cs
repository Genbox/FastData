using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Shared;
using Genbox.FastFilter;

namespace Genbox.FastData.Generator.CSharp.Benchmarks.Benchmarks;

[MemoryDiagnoser(false)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class DataStructureBenchmarks
{
    // We have both a string[] and object[] variant because the different APIs has different requirements
    private readonly object[] _data = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];
    private readonly string[] _dataStr = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];
    private readonly object[] _dataUniqLen = ["a", "aa", "bbb", "cccc", "aaaaa", "cccccc", "ddddddd", "00000000", "uuuuuuuuu", "aaaaaaaaaa"];
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
    private IFastSet<string> _fastConditionalIf = null!;
    private IFastSet<string> _fastConditionalSwitch = null!;
    private IFastSet<string> _fastMinimalPerfectHash = null!;
    private IFastSet<string> _fastHashSetLinear = null!;
    private IFastSet<string> _fastHashSetChain = null!;
    private IFastSet<string> _fastKeyLength = null!;
    private IFastSet<string> _fastKeyLengthUniqIf = null!;
    private IFastSet<string> _fastKeyLengthUniqSwitch = null!;

    [Params(1_000, 10_000)]
    public int QuerySize { get; set; }

    private IFastSet<string> CreateFastData(DataStructure ds, object[] data, Action<CSharpGeneratorConfig>? configure = null)
    {
        FastDataConfig config = new FastDataConfig(ds.ToString(), data);

        CSharpGeneratorConfig generatorConfig = new CSharpGeneratorConfig();
        generatorConfig.ClassType = ClassType.Instance;
        configure?.Invoke(generatorConfig);

        string code = FastDataGenerator.Generate(ds, config, new CSharpCodeGenerator(generatorConfig));

        return CodeGenerator.CreateFastSet<string>(code, true);
    }

    private void SetupQueries()
    {
        _queries = new string[QuerySize];

        //Half the queries are within the set
        int i;
        for (i = 0; i < QuerySize / 2; i++)
            _queries[i] = (string)_data[Random.Shared.Next(1, _data.Length)];

        //Half the queries are outside the set
        for (; i < QuerySize; i++)
            _queries[i] = "item" + Random.Shared.Next(_data.Length + 1, int.MaxValue).ToString(NumberFormatInfo.InvariantInfo);
    }

    [GlobalSetup(Target = nameof(QueryArray))]
    public void SetupArray()
    {
        SetupQueries();
        _array = _dataStr;
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
        _fastArray = CreateFastData(DataStructure.Array, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticBinarySearch))]
    public void SetupStaticBinarySearch()
    {
        SetupQueries();
        _fastBinarySearch = CreateFastData(DataStructure.BinarySearch, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticEytzingerSearch))]
    public void SetupStaticEytzingerSearch()
    {
        SetupQueries();
        _fastEytzingerSearch = CreateFastData(DataStructure.EytzingerSearch, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticConditionalIf))]
    public void SetupStaticConditionalIf()
    {
        SetupQueries();
        _fastConditionalIf = CreateFastData(DataStructure.Conditional, _data, config => config.ConditionalBranchType = BranchType.If);
    }

    [GlobalSetup(Target = nameof(QueryStaticConditionalSwitch))]
    public void SetupStaticConditionalSwitch()
    {
        SetupQueries();
        _fastConditionalSwitch = CreateFastData(DataStructure.Conditional, _data, config => config.ConditionalBranchType = BranchType.Switch);
    }

    [GlobalSetup(Target = nameof(QueryStaticMinimalPerfectHash))]
    public void SetupStaticMinimalPerfectHash()
    {
        SetupQueries();
        _fastMinimalPerfectHash = CreateFastData(DataStructure.MinimalPerfectHash, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticHashSetLinear))]
    public void SetupStaticHashSetLinear()
    {
        SetupQueries();
        _fastHashSetLinear = CreateFastData(DataStructure.HashSetLinear, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticHashSetChain))]
    public void SetupStaticHashSetChain()
    {
        SetupQueries();
        _fastHashSetChain = CreateFastData(DataStructure.HashSetChain, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticKeyLengthUniqIf))]
    public void SetupStaticKeyLengthUniqIf()
    {
        SetupQueries();
        _fastKeyLengthUniqIf = CreateFastData(DataStructure.KeyLength, _dataUniqLen, c => c.KeyLengthUniqBranchType = BranchType.If);
    }

    [GlobalSetup(Target = nameof(QueryStaticKeyLengthUniqSwitch))]
    public void SetupStaticKeyLengthUniqSwitch()
    {
        SetupQueries();
        _fastKeyLengthUniqSwitch = CreateFastData(DataStructure.KeyLength, _dataUniqLen, c => c.KeyLengthUniqBranchType = BranchType.Switch);
    }

    [GlobalSetup(Target = nameof(QueryStaticKeyLength))]
    public void SetupStaticKeyLength()
    {
        SetupQueries();
        _fastKeyLength = CreateFastData(DataStructure.KeyLength, _data);
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
    public void QueryStaticConditionalIf() => DoQuery(_fastConditionalIf.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticConditionalSwitch() => DoQuery(_fastConditionalSwitch.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticMinimalPerfectHash() => DoQuery(_fastMinimalPerfectHash.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticHashSetLinear() => DoQuery(_fastHashSetLinear.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticHashSetChain() => DoQuery(_fastHashSetChain.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticKeyLength() => DoQuery(_fastKeyLength.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticKeyLengthUniqIf() => DoQuery(_fastKeyLengthUniqIf.Contains);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticKeyLengthUniqSwitch() => DoQuery(_fastKeyLengthUniqSwitch.Contains);

    private HashSet<string> CreateHashSet() => new HashSet<string>(_dataStr, StringComparer.Ordinal);
    private FrozenSet<string> CreateFrozenSet() => CreateHashSet().ToFrozenSet();
    private BloomFilter<string> CreateBloomFilter() => new BloomFilter<string>(_dataStr);
    private BlockedBloomFilter<string> CreateBlockedBloomFilter() => new BlockedBloomFilter<string>(_dataStr);
    private BinaryFuse8Filter<string> CreateBinaryFuse8Filter() => new BinaryFuse8Filter<string>(_dataStr);

    private void DoQuery(Func<string, bool> query)
    {
        for (int i = 0; i < _queries.Length; i++)
            query(_queries[i]);
    }
}