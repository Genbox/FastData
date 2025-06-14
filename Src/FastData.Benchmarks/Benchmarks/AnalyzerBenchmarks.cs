using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.InternalShared.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class AnalyzerBenchmarks
{
    private readonly GPerfAnalyzer _analyzer;
    private readonly string[] _data;

    public AnalyzerBenchmarks()
    {
        Random rng = new Random(42);
        _data = Enumerable.Range(1, 100).Select(_ => TestHelper.GenerateRandomString(rng, 50)).ToArray();
        StringProperties props = DataAnalyzer.GetStringProperties(_data);

        _analyzer = new GPerfAnalyzer(_data.Length, props, new GPerfAnalyzerConfig(), new Simulator(), NullLogger<GPerfAnalyzer>.Instance);
    }

    [Benchmark]
    public void GPerfAnalyzer() => _analyzer.GetCandidates(_data, OnCandidateFound);

    private static bool OnCandidateFound(Candidate candidate)
    {
        // Do nothing
        return true;
    }
}