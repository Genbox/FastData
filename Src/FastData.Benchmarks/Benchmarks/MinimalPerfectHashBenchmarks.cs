using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Order;
using Genbox.FastData.Benchmarks.Code;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.InternalShared;
using Genbox.FastHash;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class MinimalPerfectHashBenchmarks
{
    private uint[] _data = null!;

    [Params(1)]
    public uint MaxCandidates { get; set; }

    [Params(8)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = StringHelper.GetIntegers(TestData.Words.OrderBy(_ => Random.Shared.Next()).Take(Length));
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetFunctions))]
    public uint[] TimeToConstruct(MixSpec spec)
    {
        return MPHHelper.Generate(_data, (hash, seed) => spec.Function(hash, seed), MaxCandidates, uint.MaxValue, _data.Length * 2).ToArray();
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetFunctions))]
    public uint[] TimeToConstructMinimal(MixSpec spec)
    {
        return MPHHelper.Generate(_data, (hash, seed) => spec.Function(hash, seed), MaxCandidates).ToArray();
    }

    public static IEnumerable<MixSpec> GetFunctions()
    {
        yield return new MixSpec(nameof(MixFunctions.Murmur_32_Seed), MixFunctions.Murmur_32_Seed);
        yield return new MixSpec(nameof(MixFunctions.XXH2_32_Seed), MixFunctions.XXH2_32_Seed);
        yield return new MixSpec(nameof(LemireMod), LemireMod);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint LemireMod(uint h, uint seed) => MathHelper.FastMod(h, seed, 17943479174021665110);
}