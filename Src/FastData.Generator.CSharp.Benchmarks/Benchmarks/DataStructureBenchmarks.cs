using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.InternalShared;
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

    private Func<string, bool> _fastArray = null!;
    private Func<string, bool> _fastBinarySearch = null!;
    private Func<string, bool> _fastEytzingerSearch = null!;
    private Func<string, bool> _fastConditionalIf = null!;
    private Func<string, bool> _fastConditionalSwitch = null!;
    private Func<string, bool> _fastPerfectHashGPerf = null!;
    private Func<string, bool> _fastPerfectHashBruteForce = null!;
    private Func<string, bool> _fastHashSetLinear = null!;
    private Func<string, bool> _fastHashSetChain = null!;
    private Func<string, bool> _fastKeyLength = null!;
    private Func<string, bool> _fastKeyLengthUniqIf = null!;
    private Func<string, bool> _fastKeyLengthUniqSwitch = null!;

    [Params(10_000)]
    public int QuerySize { get; set; }

    private static Func<string, bool> CreateFastData(StructureType ds, object[] data, Action<CSharpGeneratorConfig>? configure = null)
    {
        CSharpGeneratorConfig usrCfg = new CSharpGeneratorConfig(ds.ToString());
        configure?.Invoke(usrCfg);

        if (!FastDataGenerator.TryGenerate(data, new FastDataConfig(ds), new CSharpCodeGenerator(usrCfg), out string? source))
            throw new InvalidOperationException("Unable to create data structure: " + ds);

        return CompilationHelper.GetDelegate<Func<string, bool>>(source, true);
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
        _fastArray = CreateFastData(StructureType.Array, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticBinarySearch))]
    public void SetupStaticBinarySearch()
    {
        SetupQueries();
        _fastBinarySearch = CreateFastData(StructureType.BinarySearch, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticEytzingerSearch))]
    public void SetupStaticEytzingerSearch()
    {
        SetupQueries();
        _fastEytzingerSearch = CreateFastData(StructureType.EytzingerSearch, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticConditionalIf))]
    public void SetupStaticConditionalIf()
    {
        SetupQueries();
        _fastConditionalIf = CreateFastData(StructureType.Conditional, _data, config => config.ConditionalBranchType = BranchType.If);
    }

    [GlobalSetup(Target = nameof(QueryStaticConditionalSwitch))]
    public void SetupStaticConditionalSwitch()
    {
        SetupQueries();
        _fastConditionalSwitch = CreateFastData(StructureType.Conditional, _data, config => config.ConditionalBranchType = BranchType.Switch);
    }

    [GlobalSetup(Target = nameof(QueryStaticPerfectHashGPerf))]
    public void SetupStaticPerfectHashGPerf()
    {
        SetupQueries();
        _fastPerfectHashGPerf = CreateFastData(StructureType.PerfectHashGPerf, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticPerfectHashBruteforce))]
    public void SetupStaticPerfectHashBruteForce()
    {
        SetupQueries();
        _fastPerfectHashBruteForce = CreateFastData(StructureType.PerfectHashBruteForce, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticHashSetLinear))]
    public void SetupStaticHashSetLinear()
    {
        SetupQueries();
        _fastHashSetLinear = CreateFastData(StructureType.HashSetLinear, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticHashSetChain))]
    public void SetupStaticHashSetChain()
    {
        SetupQueries();
        _fastHashSetChain = CreateFastData(StructureType.HashSetChain, _data);
    }

    [GlobalSetup(Target = nameof(QueryStaticKeyLengthUniqIf))]
    public void SetupStaticKeyLengthUniqIf()
    {
        SetupQueries();
        _fastKeyLengthUniqIf = CreateFastData(StructureType.KeyLength, _dataUniqLen, c => c.KeyLengthUniqBranchType = BranchType.If);
    }

    [GlobalSetup(Target = nameof(QueryStaticKeyLengthUniqSwitch))]
    public void SetupStaticKeyLengthUniqSwitch()
    {
        SetupQueries();
        _fastKeyLengthUniqSwitch = CreateFastData(StructureType.KeyLength, _dataUniqLen, c => c.KeyLengthUniqBranchType = BranchType.Switch);
    }

    [GlobalSetup(Target = nameof(QueryStaticKeyLength))]
    public void SetupStaticKeyLength()
    {
        SetupQueries();
        _fastKeyLength = CreateFastData(StructureType.KeyLength, _data);
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
    public void QueryStaticArray() => DoQuery(_fastArray);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticBinarySearch() => DoQuery(_fastBinarySearch);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticEytzingerSearch() => DoQuery(_fastEytzingerSearch);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticConditionalIf() => DoQuery(_fastConditionalIf);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticConditionalSwitch() => DoQuery(_fastConditionalSwitch);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticPerfectHashGPerf() => DoQuery(_fastPerfectHashGPerf);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticPerfectHashBruteforce() => DoQuery(_fastPerfectHashBruteForce);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticHashSetLinear() => DoQuery(_fastHashSetLinear);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticHashSetChain() => DoQuery(_fastHashSetChain);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticKeyLength() => DoQuery(_fastKeyLength);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticKeyLengthUniqIf() => DoQuery(_fastKeyLengthUniqIf);

    [Benchmark, BenchmarkCategory("Query")]
    public void QueryStaticKeyLengthUniqSwitch() => DoQuery(_fastKeyLengthUniqSwitch);

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