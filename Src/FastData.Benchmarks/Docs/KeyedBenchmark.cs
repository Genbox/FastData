using System.Collections.Frozen;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Genbox.FastData.Benchmarks.Code;

namespace Genbox.FastData.Benchmarks.Docs;

/// <summary>Benchmark used in Readme</summary>
[Config(typeof(CustomConfig))]
public class KeyedBenchmark
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

    private static readonly (string, byte)[] _array = [("Labrador", 1), ("German Shepherd", 2), ("Golden Retriever", 3)];
    private static readonly Dictionary<string, byte> _dict = new Dictionary<string, byte>(_array.Select(x => new KeyValuePair<string, byte>(x.Item1, x.Item2)), StringComparer.Ordinal);
    private static readonly FrozenDictionary<string, byte> _frozen = _dict.ToFrozenDictionary();

    [BenchmarkCategory("InSet"), Benchmark(Baseline = true)]
    public bool Dictionary() => _dict.TryGetValue("German Shepherd", out _);

    [BenchmarkCategory("InSet"), Benchmark]
    public bool FrozenDictionary() => _frozen.TryGetValue("German Shepherd", out _);

    [BenchmarkCategory("InSet"), Benchmark]
    public bool FastData() => Dogs.TryLookup("German Shepherd", out _);

    [BenchmarkCategory("NotInSet"), Benchmark(Baseline = true)]
    public bool DictionaryNF() => _dict.TryGetValue("Beagle", out _);

    [BenchmarkCategory("NotInSet"), Benchmark]
    public bool FrozenDictionaryNF() => _frozen.TryGetValue("Beagle", out _);

    [BenchmarkCategory("NotInSet"), Benchmark]
    public bool FastDataNF() => Dogs.TryLookup("Beagle", out _);

    private static class Dogs
    {
        private static readonly int[] _offsets =
        [
            0, 0, 0, 0, 0, 0, 0, 1, 2
        ];

        private static readonly byte[] _values =
        [
            1, 2, 3
        ];
        private static readonly string[] _keys =
        [
            "Labrador", "", "", "", "", "", "", "German Shepherd", "Golden Retriever"
        ];

        public static bool TryLookup(string key, out byte value)
        {
            if ((49280UL & (1UL << (key.Length - 1))) == 0)
            {
                value = 0;
                return false;
            }

            int idx = key.Length - 8;
            if (StringComparer.Ordinal.Equals(key, _keys[idx]))
            {
                value = _values[_offsets[idx]];
                return true;
            }

            value = 0;
            return false;
        }
    }
}