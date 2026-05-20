using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.InternalShared.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class GPerfAnalyzerBenchmarks
{
    private readonly string[] _data;
    private readonly StringKeyProperties _props;
    private readonly Simulator _simulator;

    public GPerfAnalyzerBenchmarks()
    {
        Random rng = new Random(42);
        _data = Enumerable.Range(1, 100).Select(_ => TestHelper.GenerateRandomString(rng, 50)).ToArray();

        _props = KeyAnalyzer.GetStringProperties(_data, false, GeneratorEncoding.AsciiBytes);
        _simulator = new Simulator(_data.Length, GeneratorEncoding.AsciiBytes);
    }

    [Benchmark]
    public object ConstructHash()
    {
        GPerfAnalyzer analyzer = new GPerfAnalyzer(_data.Length, _props, new GPerfAnalyzerConfig(), _simulator, NullLogger<GPerfAnalyzer>.Instance, GeneratorEncoding.AsciiBytes, false);

        foreach (Candidate candidate in analyzer.GetCandidates(_data))
            return candidate;

        throw new InvalidOperationException("GPerfAnalyzer did not produce a candidate.");
    }
}