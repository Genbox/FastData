using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.BruteForce;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Generators;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static readonly char[] LoEntropy = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    private static readonly char[] HiEntropy = Enumerable.Range(0, ushort.MaxValue).Select(i => (char)i).ToArray();
    private static readonly string[] Data = ["razzmatazz", "quizzified", "quizzifies", "zymeszymic", "buzzworthy", "vajazzling", "whizzingly", "jazzercise", "quizzeries", "quizziness", "squeezebox", "whizzbangs", "bumfuzzled", "dizzyingly", "showbizzes", "zizyphuses", "bumfuzzles", "buzzphrase", "schemozzle", "blizzardly", "kolkhoznik", "puzzlingly", "shemozzled", "zigzaggery", "zugzwanged", "belshazzar", "bemuzzling", "dazzlingly", "embezzling", "morbidezza", "pavonazzos", "puzzledoms", "schizziest", "schnozzles", "schnozzola", "shemozzles", "shimozzles", "shlemozzle", "zygomorphy", "bathmizvah", "bedazzling", "blizzarded", "chiffchaff", "embezzlers", "hazardizes", "mizzenmast", "passamezzo", "pizzicatos", "podzolized", "pozzolanic", "puzzlement", "schizotypy", "scuzzballs", "shockjocks", "sizzlingly", "unhouzzled", "zanthoxyls", "zigzagging", "blackjacks", "crackajack"];

    private static void Main()
    {
        GeneticAnalysis();
    }

    private static void BruteForce()
    {
        RunBruteForce(RunFunc(Data, 5.0, PrependString));
        RunBruteForce(RunFunc(Data, 1.0, PermuteString));
        RunBruteForce(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, LoEntropy)));
        RunBruteForce(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, HiEntropy)));
    }

    private static void RunBruteForce(string[] data, [CallerArgumentExpression(nameof(data))]string? source = null)
    {
        Console.WriteLine("###############");
        Print(data, source);
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        BruteForceAnalyzer analyzer = new BruteForceAnalyzer(data, props, new BruteForceSettings(), HashSetCode.RunSimulation);
        Candidate<BruteForceHashSpec> top1 = analyzer.Run();
        PrintCandidate(in top1);
    }

    private static void GeneticAnalysis()
    {
        RunGeneticAnalysis(RunFunc(Data, 5.0, PrependString));
        RunGeneticAnalysis(RunFunc(Data, 1.0, PermuteString));
        RunGeneticAnalysis(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, LoEntropy)));
        RunGeneticAnalysis(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, HiEntropy)));
    }

    private static void Print(string[] data, string? source) => Console.WriteLine(source + ": " + string.Join(", ", data.Take(5)));

    private static string MutateString(string str, double factor, char[] alphabet)
    {
        Debug.Assert(factor > 0 && factor <= 1);

        char[] chars = str.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (Random.Shared.NextDouble() < factor)
                chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
        }
        return new string(chars);
    }

    private static string PermuteString(string str, double factor)
    {
        Debug.Assert(factor > 0 && factor <= 1);

        char[] chars = str.ToCharArray();
        int swapCount = (int)(chars.Length * factor);

        for (int i = 0; i < swapCount; i++)
        {
            int idx1 = Random.Shared.Next(chars.Length);
            int idx2 = Random.Shared.Next(chars.Length);
            (chars[idx1], chars[idx2]) = (chars[idx2], chars[idx1]);
        }
        return new string(chars);
    }

    private static string PrependString(string str, double factor)
    {
        Debug.Assert(factor > 0 && factor <= 100);
        return new string('a', (int)factor) + str;
    }

    private static string[] RunFunc(string[] str, double factor, Func<string, double, string> func)
    {
        string[] res = new string[str.Length];

        for (int i = 0; i < str.Length; i++)
            res[i] = func(str[i], factor);

        return res;
    }

    private static void RunGeneticAnalysis(string[] data, [CallerArgumentExpression(nameof(data))]string? source = null)
    {
        Console.WriteLine("###############");
        Print(data, source);
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        GeneticAnalyzer analyzer = new GeneticAnalyzer(data, props, new GeneticSettings(), HashSetCode.RunSimulation);
        Candidate<GeneticHashSpec> top1 = analyzer.Run();
        PrintCandidate(in top1, in props);
    }

    private static void PrintHeader<T>(in Candidate<T> candidate) where T : struct, IHashSpec
    {
        Console.WriteLine("Result:");
        Console.WriteLine($"- {nameof(candidate.Fitness)}: {candidate.Fitness}");
        Console.WriteLine($"- {nameof(candidate.Metadata)}: {string.Join(", ", candidate.Metadata.Select(x => x.ToString()))}");
    }

    private static void PrintCandidate(in Candidate<GeneticHashSpec> candidate, in StringProperties props)
    {
        PrintHeader(in candidate);

        GeneticHashSpec spec = candidate.Spec;

        //We call the hash function to build the hash string
        Func<string, uint> f = spec.GetFunction();
        f(new string('a', (int)props.LengthData.Max)); //needs to be larger than the strings in the dataset

        Console.WriteLine("Hash:");
        Console.WriteLine($"- {nameof(GeneticHashSpec.MixerSeed)}: {spec.MixerSeed}");
        Console.WriteLine($"- {nameof(GeneticHashSpec.MixerIterations)}: {spec.MixerIterations}");
        Console.WriteLine($"- {nameof(GeneticHashSpec.AvalancheSeed)}: {spec.AvalancheSeed}");
        Console.WriteLine($"- {nameof(GeneticHashSpec.AvalancheIterations)}: {spec.AvalancheIterations}");
        Console.WriteLine($"- {nameof(GeneticHashSpec.Segments)}: {string.Join(", ", spec.Segments.Select(x => '[' + x.Offset.ToString(NumberFormatInfo.InvariantInfo) + '|' + x.Length.ToString(NumberFormatInfo.InvariantInfo) + '|' + x.Alignment + ']'))}");
        Console.WriteLine($"- Mixer: {GeneticHashSpec.ExpressionConverter.Instance.GetCode(spec.GetMixer())}");
        Console.WriteLine($"- Avalanche: {GeneticHashSpec.ExpressionConverter.Instance.GetCode(spec.GetAvalanche())}");
    }

    private static void PrintCandidate(in Candidate<BruteForceHashSpec> candidate)
    {
        PrintHeader(in candidate);

        BruteForceHashSpec spec = candidate.Spec;
        Console.WriteLine("Hash:");
        Console.WriteLine($"- {nameof(BruteForceHashSpec.HashFunction)}: {spec.HashFunction}");
        Console.WriteLine($"- {nameof(BruteForceHashSpec.Segments)}: {string.Join(", ", spec.Segments.Select(x => '[' + x.Offset.ToString(NumberFormatInfo.InvariantInfo) + '|' + x.Length.ToString(NumberFormatInfo.InvariantInfo) + '|' + x.Alignment + ']'))}");
    }
}