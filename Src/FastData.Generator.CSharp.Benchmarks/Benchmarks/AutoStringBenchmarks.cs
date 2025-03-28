using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.Generator.CSharp.Benchmarks.Code;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Shared;

namespace Genbox.FastData.Generator.CSharp.Benchmarks.Benchmarks;

/// <summary>
/// The idea here is to ensure that Auto is nearly-always the fastest implementation. It should exploit properties of the
/// data to gain an advantage over specifying a specific storage mode.
/// </summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
[HideColumns("set", "Method")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class AutoStringBenchmarks
{
    /* ## Test cases ##

    ### Single dataset - A set of 1 value ###
    - We expect pre-conditionals such as length check to be present as a fast-path
    - No other conditionals should exist. A simple equality check on the content of the string is expected.

    ### Tiny dataset - A set of 2 unique values ###
    - We expect an array since linear search is faster than hashing.
    - Tree structures are no good here as memory indirection is too costly. Cache coherency is king.

    ### Small dataset - A set of 16 unique values ###
    - Pretty much the same as the tiny dataset, but here linear search is probably slower than alternatives such as binary search

    ### Medium dataset - A set of 256 unique values ###
    - At this point, a hash set is probably faster than the alternatives given a fast hash function
    - Hashset's perf is dependent on the number of collisions, which we can control to a certain extent
    - Hashset's perf is also dependent on the hash, which itself is dependent on key size (for strings)

    ### Large dataset - A set of 65.536 unique values ###
    - Likely similar to the medium dataset, but the values are out of cache L1/L2 cache and might reside in L3 cache (30 MB on Adler Lake)

    ### Enormous dataset - A set of 4.194.304 unique values ###
    - This cannot reside in L3 cache, so suddenly main memory fetches becomes a bottleneck.
    - It is likely that hashing data structures will always win here

    */

    [Benchmark, ArgumentsSource(nameof(SingleData))]
    public bool Single(IFastSet<string> set, string mode, int size) => DoCheck(set);

    [Benchmark, ArgumentsSource(nameof(TinyData))]
    public bool Tiny(IFastSet<string> set, string mode, int size) => DoCheck(set);

    [Benchmark, ArgumentsSource(nameof(SmallData))]
    public bool Small(IFastSet<string> set, string mode, int size) => DoCheck(set);

    [Benchmark, ArgumentsSource(nameof(MediumData))]
    public bool Medium(IFastSet<string> set, string mode, int size) => DoCheck(set);

    [Benchmark, ArgumentsSource(nameof(LargeData))]
    public bool Large(IFastSet<string> set, string mode, int size) => DoCheck(set);

    private static bool DoCheck(IFastSet<string> set)
    {
        bool a = true;

        for (int i = 1; i < 15; i++)
            a &= set.Contains("item" + i);

        return a;
    }

    public IEnumerable<object[]> SingleData() => GetForSize(1);
    public IEnumerable<object[]> TinyData() => GetForSize(2);
    public IEnumerable<object[]> SmallData() => GetForSize(16);
    public IEnumerable<object[]> MediumData() => GetForSize(256);
    public IEnumerable<object[]> LargeData() => GetForSize(1024);

    private static IEnumerable<object[]> GetForSize(int size)
    {
        object[] data = new object[size];

        for (int i = 0; i < size; i++)
            data[i] = new string('a', i + 1);

        foreach (StorageMode mode in Enum.GetValues<StorageMode>())
        {
            FastDataConfig cfg = new FastDataConfig("MyData", data);
            cfg.StorageMode = mode;

            CSharpGeneratorConfig genCfg = new CSharpGeneratorConfig();
            genCfg.ClassType = ClassType.Instance;

            string code = FastDataGenerator.Generate(cfg, new CSharpCodeGenerator(genCfg));
            yield return [CodeGenerator.CreateFastSet<string>(code, true), mode, size];
        }

        //We have to do this as the source generator only works on object[], but these work on string[]
        string[] strData = data.Cast<string>().ToArray();

        yield return [new UnoptimizedArray(strData), nameof(UnoptimizedArray), size];
        yield return [new UnoptimizedHashSet(strData), nameof(UnoptimizedHashSet), size];
    }
}