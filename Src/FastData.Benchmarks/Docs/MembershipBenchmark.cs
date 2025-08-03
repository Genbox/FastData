using System.Collections.Frozen;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Genbox.FastData.Benchmarks.Code;

namespace Genbox.FastData.Benchmarks.Docs;

/// <summary>Benchmark used in Readme</summary>
[Config(typeof(CustomConfig))]
public class MembershipBenchmark
{
    private class CustomConfig : ManualConfig
    {
        public CustomConfig()
        {
            AddColumn(CategoriesColumn.Default);
            AddColumn(new SpeedFactorColumn());
            HideColumns("Ratio", "RatioSD", "Error", "StdDev");

            AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);
        }
    }

    private static readonly string[] _array = ["Labrador", "German Shepherd", "Golden Retriever"];
    private static readonly HashSet<string> _hashSet = new HashSet<string>(_array, StringComparer.Ordinal);
    private static readonly FrozenSet<string> _frozen = _hashSet.ToFrozenSet();

    [BenchmarkCategory("InSet"), Benchmark(Baseline = true)]
    public bool Array() => _array.Contains("German Shepherd");

    [BenchmarkCategory("InSet"), Benchmark]
    public bool HashSet() => _hashSet.Contains("German Shepherd");

    [BenchmarkCategory("InSet"), Benchmark]
    public bool FrozenSet() => _frozen.Contains("German Shepherd");

    [BenchmarkCategory("InSet"), Benchmark]
    public bool FastData() => Dogs.Contains("German Shepherd");

    [BenchmarkCategory("NotInSet"), Benchmark(Baseline = true)]
    public bool ArrayNF() => _array.Contains("Beagle");

    [BenchmarkCategory("NotInSet"), Benchmark]
    public bool HashSetNF() => _hashSet.Contains("Beagle");

    [BenchmarkCategory("NotInSet"), Benchmark]
    public bool FrozenSetNF() => _frozen.Contains("Beagle");

    [BenchmarkCategory("NotInSet"), Benchmark]
    public bool FastDataNF() => Dogs.Contains("Beagle");

    private static class Dogs
    {
        public static bool Contains(string value)
        {
            if ((49280UL & (1UL << (value.Length - 1))) == 0)
                return false;

            return value switch
            {
                "Labrador" or "German Shepherd" or "Golden Retriever" => true,
                _ => false
            };
        }
    }
}