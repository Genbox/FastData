using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Testbed.Tests;

internal static class GeneratorTest
{
    public static void TestGenerators()
    {
        string[] data = ["cake", "fish", "horse", "internet", "word", "what"];
        StringProperties props = KeyAnalyzer.GetStringProperties(data, false);

        TestGenerators(data, props, new BruteForceGenerator(8));
        TestGenerators(data, props, new EdgeGramGenerator(8));
        TestGenerators(data, props, new OffsetGenerator());
        TestGenerators(data, props, new DeltaGenerator());
    }

    private static void TestGenerators(string[] data, StringProperties props, ISegmentGenerator generator)
    {
        Console.WriteLine($"### {generator.GetType().Name}. Appropriate: {generator.IsAppropriate(props)}");
        ArraySegment[] segments = generator.Generate(props).ToArray();
        Console.WriteLine(string.Join("\n", segments));

        foreach (ArraySegment s in segments)
        {
            Console.WriteLine("------------------");
            Console.WriteLine(string.Join("\n", data.Select(x => SegmentHelper.InsertSegmentBounds(x, s))));
        }
    }
}