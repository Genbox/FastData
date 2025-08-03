using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace Genbox.FastData.Benchmarks.Docs;

/// <summary>
/// Used in docs. Graph is generated on https://www.canva.com/
/// </summary>
[Config(typeof(Config))]
public class QPSBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddColumn(StatisticColumn.OperationsPerSecond);
            HideColumns("Error", "StdDev");
        }
    }

    [Benchmark] public bool Array1() => ArrayStructure_String_1.Contains(GetQuery());
    [Benchmark] public bool Array5() => ArrayStructure_String_5.Contains(GetQuery());
    [Benchmark] public bool Array10() => ArrayStructure_String_10.Contains(GetQuery());
    [Benchmark] public bool Array50() => ArrayStructure_String_50.Contains(GetQuery());
    [Benchmark] public bool Array100() => ArrayStructure_String_100.Contains(GetQuery());
    [Benchmark] public bool Array500() => ArrayStructure_String_500.Contains(GetQuery());

    [Benchmark] public bool BinarySearch1() => BinarySearchStructure_String_1.Contains(GetQuery());
    [Benchmark] public bool BinarySearch5() => BinarySearchStructure_String_5.Contains(GetQuery());
    [Benchmark] public bool BinarySearch10() => BinarySearchStructure_String_10.Contains(GetQuery());
    [Benchmark] public bool BinarySearch50() => BinarySearchStructure_String_50.Contains(GetQuery());
    [Benchmark] public bool BinarySearch100() => BinarySearchStructure_String_100.Contains(GetQuery());
    [Benchmark] public bool BinarySearch500() => BinarySearchStructure_String_500.Contains(GetQuery());

    [Benchmark] public bool Conditional1() => ConditionalStructure_String_1.Contains(GetQuery());
    [Benchmark] public bool Conditional5() => ConditionalStructure_String_5.Contains(GetQuery());
    [Benchmark] public bool Conditional10() => ConditionalStructure_String_10.Contains(GetQuery());
    [Benchmark] public bool Conditional50() => ConditionalStructure_String_50.Contains(GetQuery());
    [Benchmark] public bool Conditional100() => ConditionalStructure_String_100.Contains(GetQuery());
    [Benchmark] public bool Conditional500() => ConditionalStructure_String_500.Contains(GetQuery());

    [Benchmark] public bool HashTable1() => HashTableStructure_String_1.Contains(GetQuery());
    [Benchmark] public bool HashTable5() => HashTableStructure_String_5.Contains(GetQuery());
    [Benchmark] public bool HashTable10() => HashTableStructure_String_10.Contains(GetQuery());
    [Benchmark] public bool HashTable50() => HashTableStructure_String_50.Contains(GetQuery());
    [Benchmark] public bool HashTable100() => HashTableStructure_String_100.Contains(GetQuery());
    [Benchmark] public bool HashTable500() => HashTableStructure_String_500.Contains(GetQuery());

    private static string GetQuery() => Random.Shared.Next(0, 1_000_000).ToString(NumberFormatInfo.InvariantInfo);
}