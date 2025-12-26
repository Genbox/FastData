using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.InternalShared.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class GPerfAnalyzerBenchmarks
{
    private readonly GPerfAnalyzer _analyzer;
    private readonly string[] _data;

    public GPerfAnalyzerBenchmarks()
    {
        Random rng = new Random(42);
        _data = Enumerable.Range(1, 100).Select(_ => TestHelper.GenerateRandomString(rng, 50)).ToArray();

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(_data, false, false);
        _analyzer = new GPerfAnalyzer(_data.Length, props, new GPerfAnalyzerConfig(), new Simulator(_data.Length, GeneratorEncoding.UTF16), NullLogger<GPerfAnalyzer>.Instance);
    }

    [Benchmark]
    public IEnumerable<object> GPerfAnalyzer() => _analyzer.GetCandidates(_data);
}