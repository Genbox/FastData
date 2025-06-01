using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class AnalyzerBenchmarks
{
    private readonly GPerfAnalyzer _analyzer;

    public AnalyzerBenchmarks()
    {
        Random rng = new Random(42);

        string[] data = Enumerable.Range(1, 100).Select(x => TestHelper.GenerateRandomString(rng, 50)).ToArray();
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        _analyzer = new GPerfAnalyzer(data, props, new GPerfAnalyzerConfig(), new Simulator(data, new SimulatorConfig()), NullLogger<GPerfAnalyzer>.Instance);
    }

    [Benchmark]
    public object GPerfAnalyzer() => _analyzer.GetCandidates();
}