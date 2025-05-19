using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Testbed.Tests;

internal static class AnalysisTest
{
    private static readonly char[] LoEntropy = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    private static readonly char[] HiEntropy = Enumerable.Range(0, ushort.MaxValue).Select(i => (char)i).ToArray();
    private static readonly string[] Data =
    [
        "razzmatazz", "quizzified", "quizzifies", "zymeszymic", "buzzworthy", "vajazzling", "whizzingly", "jazzercise", "quizzeries", "quizziness", "squeezebox", "whizzbangs",
        "bumfuzzled", "dizzyingly", "showbizzes", "zizyphuses", "bumfuzzles", "buzzphrase", "schemozzle", "blizzardly", "kolkhoznik", "puzzlingly", "shemozzled", "zigzaggery",
        "zugzwanged", "belshazzar", "bemuzzling", "dazzlingly", "embezzling", "morbidezza", "pavonazzos", "puzzledoms", "schizziest", "schnozzles", "schnozzola", "shemozzles",
        "shimozzles", "shlemozzle", "zygomorphy", "bathmizvah", "bedazzling", "blizzarded", "chiffchaff", "embezzlers", "hazardizes", "mizzenmast", "passamezzo", "pizzicatos",
        "podzolized", "pozzolanic", "puzzlement", "schizotypy", "scuzzballs", "shockjocks", "sizzlingly", "unhouzzled", "zanthoxyls", "zigzagging", "blackjacks", "crackajack"
    ];

    public static void TestAnalyzers()
    {
        string[] data = ["aab", "agedehams", "afiskenet", "oastemad", "garisestald", "karseklipning"];
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        Simulator sim = new Simulator(new SimulatorConfig { UseUtf8 = true });
        BruteForceAnalyzer bf = new BruteForceAnalyzer(data, props, new BruteForceAnalyzerConfig(), sim);
        Candidate<BruteForceArrayHash> bfCand = bf.Run();
        Console.WriteLine($"BruteForceStringHash: ({bfCand.Fitness}) {bfCand.Spec}");

        HashFunc func = bfCand.Spec.GetHashFunction();

        foreach (string d in data)
        {
            byte[] b = Encoding.UTF8.GetBytes(d);
            Console.WriteLine(d + ": " + func(ref b[0], b.Length));
        }
        Console.WriteLine();
        Console.WriteLine("C# code:");

        var compiler = new CSharpExpressionCompiler(new TypeHelper(new TypeMap(new CSharpLanguageDef().TypeDefinitions)));
        Console.WriteLine(compiler.GetCode(bfCand.Spec.BuildExpression()));

        // GeneticAnalyzer ga = new GeneticAnalyzer(data, new GeneticAnalyzerConfig(), sim);
        // Candidate<GeneticStringHash> gaCand = ga.Run();
        // Console.WriteLine($"GeneticStringHash: ({gaCand.Fitness}) {gaCand.Spec}");
        //
        // HeuristicAnalyzer ha = new HeuristicAnalyzer(data, props, new HeuristicAnalyzerConfig(), sim);
        // Candidate<HeuristicStringHash> haCand = ha.Run();
        // Console.WriteLine($"HeuristicStringHash: ({haCand.Fitness}) {haCand.Spec}");

        //Select the spec with the best fitness
        // object best = bfCand.Fitness >= gaCand.Fitness ? bfCand.Fitness >= haCand.Fitness ? bfCand.Spec : haCand.Spec :
        // gaCand.Fitness >= haCand.Fitness ? gaCand.Spec : haCand.Spec;

        // Console.WriteLine();
        // Console.WriteLine("Best: " + best);
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

        Simulator sim = new Simulator(new SimulatorConfig());
        BruteForceAnalyzer analyzer = new BruteForceAnalyzer(data, props, new BruteForceAnalyzerConfig(), sim);
        Candidate<BruteForceArrayHash> top1 = analyzer.Run();
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
        {
            res[i] = func(str[i], factor);
        }

        return res;
    }

    private static void RunGeneticAnalysis(string[] data, [CallerArgumentExpression(nameof(data))]string? source = null)
    {
        Console.WriteLine("###############");
        Print(data, source);
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        Simulator sim = new Simulator(new SimulatorConfig());
        GeneticAnalyzer analyzer = new GeneticAnalyzer(data, new GeneticAnalyzerConfig(), sim);
        Candidate<GeneticArrayHash> top1 = analyzer.Run();
        PrintCandidate(in top1, in props);
    }

    private static void PrintHeader<T>(in Candidate<T> candidate) where T : IArrayHash
    {
        Console.WriteLine("Result:");
        Console.WriteLine($"- {nameof(candidate.Fitness)}: {candidate.Fitness}");
        Console.WriteLine($"- {nameof(candidate.Metadata)}: {string.Join(", ", candidate.Metadata.Select(x => x.ToString()))}");
    }

    private static void PrintCandidate(in Candidate<GeneticArrayHash> candidate, in StringProperties props)
    {
        PrintHeader(in candidate);

        GeneticArrayHash spec = candidate.Spec;

        Console.WriteLine("Hash:");
        Console.WriteLine($"- {nameof(GeneticArrayHash.MixerSeed)}: {spec.MixerSeed}");
        Console.WriteLine($"- {nameof(GeneticArrayHash.MixerIterations)}: {spec.MixerIterations}");
        Console.WriteLine($"- {nameof(GeneticArrayHash.AvalancheSeed)}: {spec.AvalancheSeed}");
        Console.WriteLine($"- {nameof(GeneticArrayHash.AvalancheIterations)}: {spec.AvalancheIterations}");

        Console.WriteLine($"- Mixer: {ExpressionHelper.Print(spec.GetMixer())}");
        Console.WriteLine($"- Avalanche: {ExpressionHelper.Print(spec.GetAvalanche())}");
    }

    private static void PrintCandidate(in Candidate<BruteForceArrayHash> candidate)
    {
        PrintHeader(in candidate);

        BruteForceArrayHash spec = candidate.Spec;
        Console.WriteLine("Hash:");
        Console.WriteLine($"- {nameof(BruteForceArrayHash.Segment)}: {spec.Segment.ToString()}");

        Console.WriteLine($"- Mixer: {ExpressionHelper.Print(spec.Mixer)}");
        Console.WriteLine($"- Avalanche: {ExpressionHelper.Print(spec.Avalanche)}");
    }
}