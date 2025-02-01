using System.Globalization;
using System.Numerics;
using System.Text;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        RunGeneticAnalysis();
    }

    private static void RunGeneticAnalysis()
    {
        string[] data = Enumerable.Range(1, 1500).Select(x => "aaaa" + x /*StringHelper.GenerateRandomString(Random.Shared.Next(5, 23))*/).ToArray();
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        GeneticAnalysis analyzer = new GeneticAnalysis(new GeneticSettings(), data, props);
        Candidate top1 = analyzer.Run();
        PrintCandidate(in top1, in props);
    }

    private static void PrintCandidate(in Candidate candidate, in StringProperties props)
    {
        Console.WriteLine("###############");
        Console.WriteLine("Result:");
        Console.WriteLine($"- {nameof(Candidate.Fitness)}: {candidate.Fitness}");
        Console.WriteLine($"- {nameof(Candidate.Metadata)}: {string.Join(", ", candidate.Metadata.Select(x => x.ToString()))}");

        HashSpec spec = candidate.Spec;
        spec.HashString = new StringBuilder();

        var f = HashHelper.GetHashFunction(ref spec);
        f(new string('a', (int)props.LengthData.Max)); //needs to be larger than the strings in the dataset

        Console.WriteLine("Hash:");
        Console.WriteLine($"- {nameof(HashSpec.MixerSeed)}: {spec.MixerSeed}");
        Console.WriteLine($"- {nameof(HashSpec.MixerIterations)}: {spec.MixerIterations}");
        Console.WriteLine($"- {nameof(HashSpec.AvalancheSeed)}: {spec.AvalancheSeed}");
        Console.WriteLine($"- {nameof(HashSpec.AvalancheIterations)}: {spec.AvalancheIterations}");
        Console.WriteLine($"- {nameof(HashSpec.Segments)}: {string.Join(", ", spec.Segments.Select(x => '[' + x.Offset.ToString(NumberFormatInfo.InvariantInfo) + '|' + x.Length.ToString(NumberFormatInfo.InvariantInfo) + '|' + x.Alignment + ']'))}");
        Console.WriteLine($"- {nameof(HashSpec.Seed)}: {spec.Seed}");
        Console.WriteLine($"- Func: {spec.HashString}");
    }
}