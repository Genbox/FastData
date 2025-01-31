using System.Globalization;
using System.Numerics;
using System.Text;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static readonly Random _random = new Random();

    private static void Main()
    {
        string[] inputs = new string[100];

        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = GenerateMixedEntropyString();

        // Run the segment mapper that uses char delta map
        DeltaGenerator gen = new DeltaGenerator();
        StringProperties stringProps = DataAnalyzer.GetStringProperties(inputs);

        foreach (StringSegment segment in gen.Generate(stringProps))
        {
            List<char> lst = inputs[0].ToList();
            lst.Insert(segment.Offset, '[');
            lst.Insert(segment.Offset + segment.Length + 1, ']');
            Console.WriteLine(new string(lst.ToArray()));
        }
    }

    private static string GenerateMixedEntropyString()
    {
        char[] result = new char[80];

        for (int i = 0; i < 80; i++)
        {
            double noiseValue = PerlinNoise(i * 0.5);
            if (noiseValue > 0.1)
                result[i] = (char)_random.Next(65, 90); // A-Z
            else
                result[i] = '-';
        }

        return new string(result);
    }

    private static double PerlinNoise(double x)
    {
        int xi = (int)x;
        double xf = x - xi;
        double u = xf * xf * (3.0 - 2.0 * xf);
        return Lerp(Noise(xi), Noise(xi + 1), u);
    }

    private static double Noise(int x)
    {
        x = (x << 13) ^ x;
        return unchecked(1.0 - ((x * (x * x * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0);
    }

    private static double Lerp(double a, double b, double t) => a + t * (b - a);

    private static void RunGeneticAnalysis()
    {
        string[] data = Enumerable.Range(1, 1500).Select(x => "aaaa" + x /*StringHelper.GenerateRandomString(Random.Shared.Next(5, 23))*/).ToArray();
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        GeneticAnalysis analyzer = new GeneticAnalysis(new GeneticSettings());

        Candidate top1 = analyzer.Run(data, props);
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