using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;
using Genbox.FastData.InternalShared;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class SegmentGeneratorsBenchmarks
{
    //We start at 8 and go up to 100 to cover as many cases as possible
    private readonly StringProperties _props = DataAnalyzer.GetStringProperties(Enumerable.Range(8, 100).Select(x => TestHelper.GenerateRandomString(Random.Shared, x)).ToArray());
    private readonly BruteForceGenerator _bfGen = new BruteForceGenerator(8);
    private readonly EdgeGramGenerator _egGen = new EdgeGramGenerator(8);
    private readonly DeltaGenerator _deltaGen = new DeltaGenerator();
    private readonly OffsetGenerator _ofGen = new OffsetGenerator();

    [Benchmark]
    public ArraySegment[] BruteForceGenerator() => _bfGen.Generate(_props).ToArray();

    [Benchmark]
    public ArraySegment[] EdgeGramGenerator() => _egGen.Generate(_props).ToArray();

    [Benchmark]
    public ArraySegment[] DeltaGenerator() => _deltaGen.Generate(_props).ToArray();

    [Benchmark]
    public ArraySegment[] OffsetGenerator() => _ofGen.Generate(_props).ToArray();
}