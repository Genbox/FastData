using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class SegmentGeneratorsBenchmarks
{
    private readonly BruteForceGenerator _bfGen = new BruteForceGenerator(8);
    private readonly DeltaGenerator _deltaGen = new DeltaGenerator();
    private readonly EdgeGramGenerator _egGen = new EdgeGramGenerator(8);
    private readonly OffsetGenerator _ofGen = new OffsetGenerator();

    //We start at 8 and go up to 100 to cover as many cases as possible
    private readonly StringKeyProperties _props = KeyAnalyzer.GetStringProperties(Enumerable.Range(8, 100).Select(x => TestHelper.GenerateRandomString(Random.Shared, x)).ToArray(), false, false, GeneratorEncoding.UTF16);

    [Benchmark]
    public object BruteForceGenerator() => _bfGen.Generate(_props).ToArray();

    [Benchmark]
    public object EdgeGramGenerator() => _egGen.Generate(_props).ToArray();

    [Benchmark]
    public object DeltaGenerator() => _deltaGen.Generate(_props).ToArray();

    [Benchmark]
    public object OffsetGenerator() => _ofGen.Generate(_props).ToArray();
}