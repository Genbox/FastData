using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static readonly char[] LoEntropy = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    // private static readonly char[] HiEntropy = Enumerable.Range(0, ushort.MaxValue).Select(i => (char)i).ToArray();

    private static void Main()
    {
        string[] nonRandom = ["razzmatazz", "quizzified", "quizzifies", "zymeszymic", "buzzworthy", "vajazzling", "whizzingly", "jazzercise", "quizzeries", "quizziness", "squeezebox", "whizzbangs", "bumfuzzled", "dizzyingly", "showbizzes", "zizyphuses", "bumfuzzles", "buzzphrase", "schemozzle", "blizzardly", "kolkhoznik", "puzzlingly", "shemozzled", "zigzaggery", "zugzwanged", "belshazzar", "bemuzzling", "dazzlingly", "embezzling", "morbidezza", "pavonazzos", "puzzledoms", "schizziest", "schnozzles", "schnozzola", "shemozzles", "shimozzles", "shlemozzle", "zygomorphy", "bathmizvah", "bedazzling", "blizzarded", "chiffchaff", "embezzlers", "hazardizes", "mizzenmast", "passamezzo", "pizzicatos", "podzolized", "pozzolanic", "puzzlement", "schizotypy", "scuzzballs", "shockjocks", "sizzlingly", "unhouzzled", "zanthoxyls", "zigzagging", "blackjacks", "crackajack"];

        // string[] prepend100 = RunFunc(nonRandom, 1.0, PrependString);
        // Print(prepend100);


        // Print(nonRandom);
        // string[] permute001 = RunFunc(nonRandom, 0.1, PermuteString);
        // Print(permute001);
        // string[] permute050 = RunFunc(nonRandom, 0.5, PermuteString);
        // Print(permute050);
        // string[] permute100 = RunFunc(nonRandom, 1.0, PermuteString);
        // Print(permute100);
        //
        // string[] mutateLo001 = RunFunc(nonRandom, 0.1, (x, y) => MutateString(x, y, LoEntropy));
        // Print(mutateLo001);
        // string[] mutateLo050 = RunFunc(nonRandom, 0.5, (x, y) => MutateString(x, y, LoEntropy));
        // Print(mutateLo050);
        string[] mutateLo100 = RunFunc(nonRandom, 1.0, (x, y) => MutateString(x, y, LoEntropy));
        RunGeneticAnalysis(mutateLo100);

        // Print(mutateLo100);
        //
        // string[] mutateSinLo001 = RunFunc(nonRandom, 0.1, (x, y) => SineMutateString(x, y, LoEntropy));
        // Print(mutateSinLo001);
        // string[] mutateSinLo050 = RunFunc(nonRandom, 0.5, (x, y) => SineMutateString(x, y, LoEntropy));
        // Print(mutateSinLo050);
        // string[] mutateSinLo100 = RunFunc(nonRandom, 1.0, (x, y) => SineMutateString(x, y, LoEntropy));
        // Print(mutateSinLo100);
        //
        // string[] mutateHi001 = RunFunc(nonRandom, 0.1, (x, y) => MutateString(x, y, HiEntropy));
        // Print(mutateHi001);
        // string[] mutateHi050 = RunFunc(nonRandom, 0.5, (x, y) => MutateString(x, y, HiEntropy));
        // Print(mutateHi050);
        // string[] mutateHi100 = RunFunc(nonRandom, 1.0, (x, y) => MutateString(x, y, HiEntropy));
        // RunGeneticAnalysis(mutateHi100);

        // Print(mutateHi100);
        //
        // string[] mutateSinHi001 = RunFunc(nonRandom, 0.1, (x, y) => SineMutateString(x, y, HiEntropy));
        // Print(mutateSinHi001);
        // string[] mutateSinHi050 = RunFunc(nonRandom, 0.5, (x, y) => SineMutateString(x, y, HiEntropy));
        // Print(mutateSinHi050);
        // string[] mutateSinHi100 = RunFunc(nonRandom, 1.0, (x, y) => SineMutateString(x, y, HiEntropy));
        // Print(mutateSinHi100);
    }

    private static void Print(string[] data, [CallerArgumentExpression(nameof(data))]string? source = null)
    {
        Console.WriteLine(source);

        for (int i = 0; i < data.Length; i++)
            Console.WriteLine(i + ". " + data[i]);
    }

    private static string SineMutateString(string str, double factor, char[] alphabet)
    {
        Debug.Assert(factor > 0 && factor <= 1);

        char[] chars = str.ToCharArray();
        int length = chars.Length;
        for (int i = 0; i < length; i++)
        {
            double sineValue = (Math.Sin((double)i / length * Math.PI * 2) + 1) / 2;
            if (Random.Shared.NextDouble() < sineValue * factor)
                chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
        }
        return new string(chars);
    }

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
        Debug.Assert(factor > 0 && factor <= 1);

        int count = (int)(10 * (Random.Shared.NextDouble() * factor));
        return new string('a', count) + str;
    }

    private static string[] RunFunc(string[] str, double factor, Func<string, double, string> func)
    {
        string[] res = new string[str.Length];

        for (int i = 0; i < str.Length; i++)
            res[i] = func(str[i], factor);

        return res;
    }

    private static void RunGeneticAnalysis(string[] data)
    {
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        GeneticHashAnalyzer analyzer = new GeneticHashAnalyzer(new GeneticSettings(), data, props);
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