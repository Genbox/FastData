using System.Globalization;
using System.Text.RegularExpressions;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class Program
{
    private static readonly (string Name, Func<DockerManager, BenchmarkBase> Factory)[] HarnessFactories =
    [
        ("CSharp", x => new CSharpBenchmark(x)),
        ("CPlusPlus", x => new CPlusPlusBenchmark(x)),
        ("Rust", x => new RustBenchmark(x))
    ];

    private static async Task<int> Main(string[] args)
    {
        if (!TryParseArgs(args, out string[] filters, out bool showHelp, out bool dryRun))
            return 1;

        if (showHelp)
        {
            PrintUsage();
            return 0;
        }

        ITestData[] benchmarkData = TestVectorHelper.GetBenchmarkData().ToArray();

        if (dryRun)
            return DryRun(benchmarkData, filters);

        CpuSelection? cpuSelection = CpuSelector.TryGetSelection();

        if (cpuSelection is null)
            Console.WriteLine("Benchmark CPU selection: using default CPU set 0.");
        else
            Console.WriteLine($"Benchmark CPU selection: {cpuSelection.CpuSet} (core {cpuSelection.PhysicalCoreIndex}, siblings {cpuSelection.Siblings}, logical {cpuSelection.LogicalProcessorCount}, cores {cpuSelection.PhysicalCoreCount}).");

        string cpuSet = cpuSelection?.CpuSet ?? "0";
        bool ranAny = false;

        foreach ((string name, Func<DockerManager, BenchmarkBase> factory) in HarnessFactories)
        {
            ITestData[] selectedData = GetSelectedData(name, benchmarkData, filters);

            if (selectedData.Length == 0)
                continue;

            ranAny = true;
            await RunHarnessAsync(factory, selectedData, cpuSet, CancellationToken.None);
        }

        if (!ranAny)
        {
            await Console.Error.WriteLineAsync($"No benchmarks matched filter(s): {string.Join(", ", filters)}").ConfigureAwait(false);
            return 1;
        }

        return 0;
    }

    private static async ValueTask RunHarnessAsync(Func<DockerManager, BenchmarkBase> harnessFactory, IEnumerable<ITestData> benchmarkData, string cpuSet, CancellationToken cancellationToken)
    {
        await using DockerManager dockerManager = new DockerManager(cpuSet: cpuSet);
        BenchmarkBase harness = harnessFactory(dockerManager);

        foreach (ITestData data in benchmarkData)
        {
            BenchmarkResult result = await harness.RunAsync(data, cancellationToken);
            string min = FormatResult(result.Min);
            string median = FormatResult(result.Median);
            string max = FormatResult(result.Max);
            Console.WriteLine($"{harness.Name,-10} {data.Identifier,-30} min={min,-18} median={median,-18} max={max}");
        }
    }

    private static bool TryParseArgs(string[] args, out string[] filters, out bool showHelp, out bool dryRun)
    {
        showHelp = false;
        dryRun = false;
        List<string> parsedFilters = [];

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;
                filters = [];
                return true;
            }

            if (string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                dryRun = true;
                continue;
            }

            if (string.Equals(arg, "--filter", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-f", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 == args.Length)
                {
                    Console.Error.WriteLine($"Missing value for '{arg}'.");
                    filters = [];
                    return false;
                }

                parsedFilters.Add(args[++i]);
                continue;
            }

            const string filterPrefix = "--filter=";
            if (arg.StartsWith(filterPrefix, StringComparison.OrdinalIgnoreCase))
            {
                parsedFilters.Add(arg[filterPrefix.Length..]);
                continue;
            }

            if (arg.Length > 0 && arg[0] == '-')
            {
                Console.Error.WriteLine($"Unknown argument '{arg}'.");
                filters = [];
                return false;
            }

            parsedFilters.Add(IsHarnessName(arg) ? arg + ".*" : arg);
        }

        filters = parsedFilters.Count == 0 ? ["*"] : parsedFilters.ToArray();
        return true;
    }

    private static int DryRun(ITestData[] benchmarkData, string[] filters)
    {
        bool matchedAny = false;

        foreach ((string name, _) in HarnessFactories)
        {
            foreach (ITestData data in GetSelectedData(name, benchmarkData, filters))
            {
                matchedAny = true;
                Console.WriteLine(GetBenchmarkName(name, data));
            }
        }

        if (matchedAny)
            return 0;

        Console.Error.WriteLine($"No benchmarks matched filter(s): {string.Join(", ", filters)}");
        return 1;
    }

    private static ITestData[] GetSelectedData(string harnessName, IEnumerable<ITestData> benchmarkData, string[] filters) => benchmarkData.Where(x => MatchesAny(GetBenchmarkName(harnessName, x), filters)).ToArray();

    private static bool MatchesAny(string benchmarkName, string[] filters) => filters.Any(x => Matches(benchmarkName, x));

    private static bool Matches(string benchmarkName, string filter)
    {
        string pattern = "^" + Regex.Escape(filter).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(benchmarkName, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool IsHarnessName(string value) => HarnessFactories.Any(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));

    private static string GetBenchmarkName(string harnessName, ITestData data) => harnessName + "." + data.Identifier;

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner [language]");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner --filter <pattern>");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner --dry-run [--filter <pattern>]");
        Console.WriteLine();
        Console.WriteLine("Filters use BenchmarkDotNet-style wildcards and match Language.Identifier names, for example:");
        Console.WriteLine("  --filter \"*HashTable*\"");
        Console.WriteLine("  --filter \"CSharp.*Int32*\"");
        Console.WriteLine("  --dry-run --filter \"Rust.*Pgm*\"");
        Console.WriteLine("  CSharp");
    }

    private static string FormatResult(double value) => value.ToString("0.#################", CultureInfo.InvariantCulture);
}
