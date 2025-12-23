using System.Diagnostics;
using System.Runtime.CompilerServices;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using Serilog.Templates;

namespace Genbox.FastData.Testbed.Tests;

internal static class AnalysisTest
{
    private static readonly Logger _logConf = new LoggerConfiguration()
                                              .MinimumLevel.Debug()
                                              .WriteTo.File(
                                                  new ExpressionTemplate("[{@t:HH:mm:ss} {@l:u3}] [{substring(@p['SourceContext'], lastIndexOf(@p['SourceContext'], '.') + 1)}] {@m}\n"),
                                                  @"C:\Temp\FastData.log"
                                              )
                                              .CreateLogger();

    // private static readonly char[] LoEntropy = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    // private static readonly char[] HiEntropy = Enumerable.Range(0, ushort.MaxValue).Select(i => (char)i).ToArray();
    private static readonly string[] Data =
    [
        "razzmatazz", "quizzified", "quizzifies", "zymeszymic", "buzzworthy", "vajazzling", "whizzingly", "jazzercise", "quizzeries", "quizziness", "squeezebox", "whizzbangs",
        "bumfuzzled", "dizzyingly", "showbizzes", "zizyphuses", "bumfuzzles", "buzzphrase", "schemozzle", "blizzardly", "kolkhoznik", "puzzlingly", "shemozzled", "zigzaggery",
        "zugzwanged", "belshazzar", "bemuzzling", "dazzlingly", "embezzling", "morbidezza", "pavonazzos", "puzzledoms", "schizziest", "schnozzles", "schnozzola", "shemozzles",
        "shimozzles", "shlemozzle", "zygomorphy", "bathmizvah", "bedazzling", "blizzarded", "chiffchaff", "embezzlers", "hazardizes", "mizzenmast", "passamezzo", "pizzicatos",
        "podzolized", "pozzolanic", "puzzlement", "schizotypy", "scuzzballs", "shockjocks", "sizzlingly", "unhouzzled", "zanthoxyls", "zigzagging", "blackjacks", "crackajack"
    ];

    public static void TestBest()
    {
        StringKeyProperties props = KeyAnalyzer.GetStringProperties(Data, false);

        StringAnalyzerConfig cfg = new StringAnalyzerConfig();
        cfg.BruteForceAnalyzerConfig = null;
        cfg.GPerfAnalyzerConfig = null;

        GeneticAnalyzerConfig gcfg = new GeneticAnalyzerConfig();
        cfg.GeneticAnalyzerConfig = gcfg;

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 1; i < 100; i++)
        {
            gcfg.MaxGenerations = i;

            for (int j = 5; j < 100; j++)
            {
                gcfg.PopulationSize = j;

                sw.Restart();
                Candidate cand = FastDataGenerator.GetBestHash(Data, props, cfg, NullLoggerFactory.Instance, GeneratorEncoding.UTF16, false);

                sw.Stop();
                Console.WriteLine($"{i.ToString(),-10}{j.ToString(),-10}{sw.ElapsedMilliseconds,-10:N0}{cand.Collisions,-10:N0}");
            }
        }
    }

    public static void TestNoAnalyzer()
    {
        string[] data = RunFunc(Data, 5.0, PrependString).ToArray();
        Print(data, "DefaultHash");

        Simulator sim = new Simulator(data.Length, GeneratorEncoding.UTF16);
        PrintCandidate(sim.Run(data, DefaultStringHash.UTF16Instance));
    }

    public static void TestBruteForceAnalyzer()
    {
        RunBruteForce(RunFunc(Data, 5.0, PrependString));

        // RunBruteForce(RunFunc(Data, 1.0, PermuteString));
        // RunBruteForce(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, LoEntropy)));
        // RunBruteForce(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, HiEntropy)));
    }

    public static void TestGeneticAnalyzer()
    {
        RunGeneticAnalysis(RunFunc(Data, 5.0, PrependString));

        // RunGeneticAnalysis(RunFunc(Data, 1.0, PermuteString));
        // RunGeneticAnalysis(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, LoEntropy)));
        // RunGeneticAnalysis(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, HiEntropy)));
    }

    public static void TestGPerfAnalyzer()
    {
        RunGPerfAnalysis(RunFunc(Data, 5.0, PrependString));

        // RunGeneticAnalysis(RunFunc(Data, 1.0, PermuteString));
        // RunGeneticAnalysis(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, LoEntropy)));
        // RunGeneticAnalysis(RunFunc(Data, 1.0, (x, y) => MutateString(x, y, HiEntropy)));
    }

    private static void RunBruteForce(string[] data, [CallerArgumentExpression(nameof(data))]string? source = null)
    {
        Print(data, source);

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, false);
        using SerilogLoggerFactory loggerFactory = new SerilogLoggerFactory(_logConf);
        BruteForceAnalyzer analyzer = new BruteForceAnalyzer(props, new BruteForceAnalyzerConfig(), new Simulator(data.Length, GeneratorEncoding.UTF16), loggerFactory.CreateLogger<BruteForceAnalyzer>());
        PrintCandidate(analyzer.GetCandidates(data).OrderByDescending(x => x.Fitness).FirstOrDefault());
    }

    private static void RunGeneticAnalysis(string[] data, [CallerArgumentExpression(nameof(data))]string? source = null)
    {
        Print(data, source);

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, false);
        using SerilogLoggerFactory loggerFactory = new SerilogLoggerFactory(_logConf);
        GeneticAnalyzer analyzer = new GeneticAnalyzer(props, new GeneticAnalyzerConfig(), new Simulator(data.Length, GeneratorEncoding.UTF16), loggerFactory.CreateLogger<GeneticAnalyzer>());
        PrintCandidate(analyzer.GetCandidates(data).OrderByDescending(x => x.Fitness).FirstOrDefault());
    }

    private static void RunGPerfAnalysis(string[] data, [CallerArgumentExpression(nameof(data))]string? source = null)
    {
        Print(data, source);

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, false);
        using SerilogLoggerFactory loggerFactory = new SerilogLoggerFactory(_logConf);
        GPerfAnalyzer analyzer = new GPerfAnalyzer(data.Length, props, new GPerfAnalyzerConfig(), new Simulator(data.Length, GeneratorEncoding.UTF16), loggerFactory.CreateLogger<GPerfAnalyzer>());
        PrintCandidate(analyzer.GetCandidates(data).OrderByDescending(x => x.Fitness).FirstOrDefault());
    }

    private static void Print(string[] data, string? source, [CallerMemberName]string? member = null)
    {
        Console.WriteLine("");
        Console.WriteLine($"############### {member} ###############");
        Console.WriteLine(source + ": " + string.Join(", ", data.Take(5)));
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

    private static void PrintCandidate(Candidate? candidate)
    {
        if (candidate == null)
            return;

        Console.WriteLine();
        Console.WriteLine("#### Candidate ####");
        Console.WriteLine($"{nameof(candidate.Fitness)}: {candidate.Fitness}");
        Console.WriteLine($"{nameof(candidate.Collisions)}: {candidate.Collisions}");
        Console.WriteLine($"{nameof(candidate.Time)}: {candidate.Time}");

        Console.WriteLine();
        Console.WriteLine("#### Hash ####");
        Console.WriteLine(candidate.StringHash);

        // Console.WriteLine();
        // Console.WriteLine("#### Expression ####");

        // Console.WriteLine(candidate.StringHash.GetExpression().ToReadableString());
        // Console.WriteLine(ExpressionHelper.Print(candidate.StringHash.GetExpression()));
    }
}