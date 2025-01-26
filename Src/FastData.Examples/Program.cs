using System.Globalization;
using System.Text;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        string[] data = Enumerable.Range(1, 1500).Select(x => "aaaa" + x /*StringHelper.GenerateRandomString(Random.Shared.Next(5, 23))*/).ToArray();
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        GeneticAnalysis analyzer = new GeneticAnalysis(new GeneticSettings());

        // Run a simulation that uses the default full-hash function. This is the one we have to beat.
        Candidate target = new Candidate
        {
            //https://github.com/dotnet/runtime/blob/843f7985e470f5526799abacb81cbb245c7dec70/src/libraries/System.Collections.Immutable/src/System/Collections/Frozen/String/Hashing.cs#L22
            Spec = new HashSpec
            {
                Seed = (5381 << 16) + 5381,
                ExtractorSeed = 42,
                MixerSeed = 42,
                MixerIterations = 2,
                AvalancheSeed = 42,
                AvalancheIterations = 2,
                Segments = [new StringSegment { Offset = 0 }]
            }
        };

        // target.Spec.HashString = new StringBuilder();

        // var f = HashHelper.GetHashFunction(ref target.Spec);
        // Console.WriteLine(f("aaaaaaaa"));

        analyzer.RunSimulation(data, ref target);

        Candidate top1 = analyzer.Run(data, props);
        PrintCandidate(ref target);
        PrintCandidate(ref top1);
    }

    private static void PrintCandidate(ref Candidate candidate)
    {
        Console.WriteLine("###############");
        Console.WriteLine("Result:");
        Console.WriteLine($"- {nameof(Candidate.Fitness)}: {candidate.Fitness}");
        Console.WriteLine($"- {nameof(Candidate.Metadata)}: {string.Join(", ", candidate.Metadata.Select(x => x.ToString()))}");

        HashSpec spec = candidate.Spec;
        spec.HashString = new StringBuilder();

        var f = HashHelper.GetHashFunction(ref spec);
        f("aaaaaaaa");

        Console.WriteLine("Hash:");
        Console.WriteLine($"- {nameof(HashSpec.MixerSeed)}: {spec.MixerSeed}");
        Console.WriteLine($"- {nameof(HashSpec.MixerIterations)}: {spec.MixerIterations}");
        Console.WriteLine($"- {nameof(HashSpec.AvalancheSeed)}: {spec.AvalancheSeed}");
        Console.WriteLine($"- {nameof(HashSpec.AvalancheIterations)}: {spec.AvalancheIterations}");
        Console.WriteLine($"- {nameof(HashSpec.Segments)}: {string.Join(", ", spec.Segments.Select(x => '[' + x.Offset.ToString(NumberFormatInfo.InvariantInfo) + '|' + x.Length.ToString(NumberFormatInfo.InvariantInfo) + ']'))}");
        Console.WriteLine($"- {nameof(HashSpec.Seed)}: {spec.Seed}");
        Console.WriteLine($"- Func: {spec.HashString}");
    }
}

public static class StringHelper
{
    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly Random _random = new Random(42);

    public static string GenerateRandomString(int length)
    {
        char[] data = new char[length];

        for (int i = 0; i < length; i++)
            data[i] = _alphabet[_random.Next(0, _alphabet.Length)];

        return new string(data);
    }
}