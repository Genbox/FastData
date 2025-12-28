using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class KeyAnalyzerBenchmarks
{
    private readonly string[] _data;

    public KeyAnalyzerBenchmarks()
    {
        Random rng = new Random(42);
        _data = Enumerable.Range(1, 100).Select(_ => TestHelper.GenerateRandomString(rng, 50)).ToArray();
    }

    [Benchmark]
    public object GetStringProperties() => KeyAnalyzer.GetStringProperties(_data, true, false, GeneratorEncoding.UTF16);
}