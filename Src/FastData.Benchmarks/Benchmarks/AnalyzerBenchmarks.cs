using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class AnalyzerBenchmarks
{
    private readonly HeuristicAnalyzer _analyzer;

    public AnalyzerBenchmarks()
    {
        Random rng = new Random(42);

        string[] data = Enumerable.Range(1, 100).Select(x => TestHelper.GenerateRandomString(rng, 50)).ToArray();
        StringProperties props = DataAnalyzer.GetStringProperties(data);
        _analyzer = new HeuristicAnalyzer(data, props, new HeuristicAnalyzerConfig(), new Simulator(new SimulatorConfig()));
    }

    [Benchmark]
    public object HeuristicAnalyzer() => _analyzer.Run();
}