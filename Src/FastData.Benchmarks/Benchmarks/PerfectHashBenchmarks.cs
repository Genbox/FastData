using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Order;
using Genbox.FastData.Benchmarks.Code;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class PerfectHashBenchmarks
{
    private uint[] _data = null!;

    [Params(1)]
    public uint MaxCandidates { get; set; }

    [Params(8)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = TestHelper.GetIntegers(TestData.Words.OrderBy(_ => Random.Shared.Next()).Take(Length));
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetFunctions))]
    public uint[] TimeToConstruct(MixSpec spec)
    {
        return PerfectHashHelper.Generate(_data, (obj, seed) => spec.Function(obj) ^ seed, MaxCandidates, uint.MaxValue, _data.Length * 2).ToArray();
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetFunctions))]
    public uint[] TimeToConstructMinimal(MixSpec spec)
    {
        return PerfectHashHelper.Generate(_data, (obj, seed) => spec.Function(obj) ^ seed, MaxCandidates).ToArray();
    }

    public static IEnumerable<MixSpec> GetFunctions()
    {
        yield return new MixSpec(nameof(Mixers.Murmur_32), static obj => Mixers.Murmur_32((uint)obj));
        yield return new MixSpec(nameof(Mixers.XXH2_32), static obj => Mixers.Murmur_32((uint)obj));
    }
}