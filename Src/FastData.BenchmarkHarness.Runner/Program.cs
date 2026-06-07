using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConsolePlot;
using ConsolePlot.Drawing.Tools;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class Program
{
    private const string ResultsDir = "benchmark-results";
    private const int ResultReadBufferSize = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private static readonly ConsoleColor[] PlotColors =
    [
        ConsoleColor.Cyan,
        ConsoleColor.Yellow,
        ConsoleColor.Green,
        ConsoleColor.Magenta,
        ConsoleColor.Blue,
        ConsoleColor.Red,
        ConsoleColor.White,
        ConsoleColor.DarkCyan,
        ConsoleColor.DarkYellow,
        ConsoleColor.DarkGreen,
        ConsoleColor.DarkMagenta,
        ConsoleColor.DarkBlue,
        ConsoleColor.DarkRed,
        ConsoleColor.Gray
    ];

    private static readonly (string Name, Func<DockerManager, BenchmarkBase> Factory)[] HarnessFactories =
    [
        ("CSharp", x => new CSharpBenchmark(x)),
        ("CPlusPlus", x => new CPlusPlusBenchmark(x)),
        ("Rust", x => new RustBenchmark(x))
    ];

    private static async Task<int> Main(string[] args)
    {
        if (!TryParseArgs(args, out string[] filters, out bool showHelp, out bool dryRun, out bool plot, out bool individualPlot))
            return 1;

        if (showHelp)
        {
            PrintUsage();
            return 0;
        }

        ITestData[] benchmarkData = TestVectorHelper.GetBenchmarkData().ToArray();

        if (dryRun)
            return DryRun(benchmarkData, filters);

        if (plot)
            return PlotResults(benchmarkData, filters, false);

        if (individualPlot)
            return PlotResults(benchmarkData, filters, true);

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
            string benchmarkName = GetBenchmarkName(harness.Name, data);
            BenchmarkResultEntry? previousResult = await ReadPreviousResultAsync(benchmarkName, cancellationToken).ConfigureAwait(false);
            BenchmarkResult result = await harness.RunAsync(data, cancellationToken);
            string min = FormatResult(result.Min);
            string median = FormatResult(result.Median);
            string max = FormatResult(result.Max);
            string avg = FormatResult(result.Avg);
            string medianDelta = FormatDelta(result.Median, previousResult?.Median);
            string avgDelta = FormatDelta(result.Avg, previousResult?.Avg);

            Console.WriteLine($"{harness.Name,-10} {data.Identifier,-30} min={min,-18} median={median,-18} max={max,-18} avg={avg,-18} median delta={medianDelta,-10} avg delta={avgDelta}");
            await AppendResultAsync(benchmarkName, result, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool TryParseArgs(string[] args, out string[] filters, out bool showHelp, out bool dryRun, out bool plot, out bool individualPlot)
    {
        showHelp = false;
        dryRun = false;
        plot = false;
        individualPlot = false;
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

            if (string.Equals(arg, "--plot", StringComparison.OrdinalIgnoreCase))
            {
                plot = true;
                continue;
            }

            if (string.Equals(arg, "--individual-plot", StringComparison.OrdinalIgnoreCase))
            {
                individualPlot = true;
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

    private static int PlotResults(ITestData[] benchmarkData, string[] filters, bool individual)
    {
        Console.OutputEncoding = Encoding.UTF8;
        List<BenchmarkHistory> histories = [];

        foreach ((string name, _) in HarnessFactories)
        {
            foreach (ITestData data in GetSelectedData(name, benchmarkData, filters))
            {
                string benchmarkName = GetBenchmarkName(name, data);
                BenchmarkResultEntry[] entries = ReadResultHistory(benchmarkName);

                if (entries.Length == 0)
                    continue;

                histories.Add(new BenchmarkHistory(benchmarkName, entries));
            }
        }

        if (histories.Count == 0)
        {
            Console.Error.WriteLine($"No benchmark results matched filter(s): {string.Join(", ", filters)}");
            return 1;
        }

        if (individual)
        {
            foreach (BenchmarkHistory history in histories)
                PlotBenchmarkHistory(history);
        }
        else
            PlotCombinedHistory(histories);

        return 0;
    }

    private static BenchmarkResultEntry[] ReadResultHistory(string benchmarkName)
    {
        string path = GetResultPath(benchmarkName);

        if (!File.Exists(path))
            return [];

        List<BenchmarkResultEntry> entries = [];

        foreach (string line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            BenchmarkResultEntry? entry = JsonSerializer.Deserialize<BenchmarkResultEntry>(line, JsonOptions);
            if (entry != null)
                entries.Add(entry);
        }

        return entries.ToArray();
    }

    private static void PlotCombinedHistory(List<BenchmarkHistory> histories)
    {
        Console.WriteLine();
        Console.WriteLine("Median benchmark history");

        Plot plot = new Plot(GetPlotWidth(), 20);

        for (int i = 0; i < histories.Count; i++)
        {
            BenchmarkHistory history = histories[i];
            PointPen pen = GetPlotPen(i);
            AddMedianSeries(plot, history, pen);
            WriteLegendLine(i + 1, pen.Color, history.Name, history.Entries.Length);
        }

        DrawPlot(plot, histories.SelectMany(x => x.Entries), histories.Max(x => x.Entries.Length));
    }

    private static void WriteLegendLine(int index, ConsoleColor color, string benchmarkName, int dataPointCount)
    {
        ConsoleColor previousColor = Console.ForegroundColor;

        Console.Write($"{index,2}. ");
        Console.ForegroundColor = color;
        Console.Write(benchmarkName);
        Console.ForegroundColor = previousColor;
        Console.Write($" ({dataPointCount} data points)");
        Console.WriteLine();
    }

    private static void PlotBenchmarkHistory(BenchmarkHistory history)
    {
        Console.WriteLine();
        Console.WriteLine(history.Name);

        Plot plot = new Plot(GetPlotWidth(), 20);
        AddMedianSeries(plot, history, GetPlotPen(0));
        DrawPlot(plot, history.Entries, history.Entries.Length);
    }

    private static void AddMedianSeries(Plot plot, BenchmarkHistory history, PointPen pen)
    {
        double[] xs = Enumerable.Range(1, history.Entries.Length).Select(x => (double)x).ToArray();
        double[] medians = history.Entries.Select(x => x.Median).ToArray();
        plot.AddSeries(xs, medians, pen);
    }

    private static PointPen GetPlotPen(int index) => new PointPen(SystemPointBrushes.Braille, PlotColors[index % PlotColors.Length]);

    private static void DrawPlot(Plot plot, IEnumerable<BenchmarkResultEntry> entries, int maxDataPointCount)
    {
        BenchmarkResultEntry[] entryArray = entries.ToArray();
        DateTimeOffset minTimestamp = entryArray.Min(x => x.TimestampUtc);
        DateTimeOffset maxTimestamp = entryArray.Max(x => x.TimestampUtc);

        Console.WriteLine("X axis: result number");
        Console.WriteLine("Y axis: median");
        Console.WriteLine($"Timestamp range: {FormatTimestamp(minTimestamp)} to {FormatTimestamp(maxTimestamp)}");
        plot.Axis.IsVisible = true;
        plot.Grid.IsVisible = true;
        plot.Ticks.IsVisible = true;
        plot.Ticks.DesiredXStep = GetDesiredXStep(maxDataPointCount);
        plot.Ticks.Labels.IsVisible = true;
        plot.Ticks.Labels.Format = "0";
        plot.Draw();
        plot.Render();
        return;

        string FormatTimestamp(DateTimeOffset timestamp) => timestamp.UtcDateTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss 'UTC'", CultureInfo.InvariantCulture);
    }

    private static int GetDesiredXStep(int maxDataPointCount)
    {
        int targetTickCount = Math.Clamp(maxDataPointCount, 2, 10);

        return Math.Max(8, GetPlotWidth() / targetTickCount);
    }

    private static int GetPlotWidth()
    {
        if (Console.IsOutputRedirected)
            return 100;

        return Math.Clamp(Console.WindowWidth - 1, 60, 140);
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

    private static async Task<BenchmarkResultEntry?> ReadPreviousResultAsync(string benchmarkName, CancellationToken cancellationToken)
    {
        string path = GetResultPath(benchmarkName);

        if (!File.Exists(path))
            return null;

        string? lastLine = await ReadLastLineAsync(path, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(lastLine))
            return null;

        return JsonSerializer.Deserialize<BenchmarkResultEntry>(lastLine, JsonOptions);
    }

    private static async Task AppendResultAsync(string benchmarkName, BenchmarkResult result, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(ResultsDir);
        BenchmarkResultEntry entry = new BenchmarkResultEntry(benchmarkName, result.Min, result.Median, result.Max, result.Avg, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(entry, JsonOptions);
        await File.AppendAllTextAsync(GetResultPath(benchmarkName), json + Environment.NewLine, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> ReadLastLineAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, ResultReadBufferSize, FileOptions.Asynchronous);

        if (stream.Length == 0)
            return null;

        long remaining = stream.Length;
        long? lineEnd = null;
        byte[] buffer = new byte[ResultReadBufferSize];

        while (remaining > 0)
        {
            int readLength = (int)Math.Min(buffer.Length, remaining);
            remaining -= readLength;

            stream.Seek(remaining, SeekOrigin.Begin);
            await stream.ReadExactlyAsync(buffer.AsMemory(0, readLength), cancellationToken).ConfigureAwait(false);

            ReadOnlySpan<byte> span = buffer.AsSpan(0, readLength);

            if (lineEnd is null)
            {
                int contentEnd = span.Length;
                while (contentEnd > 0 && IsNewLine(span[contentEnd - 1]))
                    contentEnd--;

                if (contentEnd == 0)
                    continue;

                lineEnd = remaining + contentEnd;
                span = span[..contentEnd];
            }

            int lineBreakIndex = span.LastIndexOfAny((byte)'\r', (byte)'\n');
            if (lineBreakIndex >= 0)
            {
                long lineStart = remaining + lineBreakIndex + 1;
                return await ReadUtf8RangeAsync(stream, lineStart, lineEnd.Value - lineStart, cancellationToken).ConfigureAwait(false);
            }

            if (remaining == 0)
                return await ReadUtf8RangeAsync(stream, 0, lineEnd.Value, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<string?> ReadUtf8RangeAsync(FileStream stream, long start, long length, CancellationToken cancellationToken)
    {
        if (length <= 0)
            return null;

        if (length > int.MaxValue)
            throw new InvalidOperationException("The last benchmark result line is too large to read.");

        byte[] line = new byte[(int)length];
        stream.Seek(start, SeekOrigin.Begin);
        await stream.ReadExactlyAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(line);
    }

    private static bool IsNewLine(byte value) => value is (byte)'\r' or (byte)'\n';

    private static string GetResultPath(string benchmarkName) => Path.Combine(ResultsDir, SanitizeFileName(benchmarkName) + ".jsonl");

    private static string SanitizeFileName(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        char[] invalidChars = Path.GetInvalidFileNameChars();

        foreach (char ch in value)
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);

        return builder.ToString();
    }

    private static string FormatDelta(double current, double? previous)
    {
        if (previous is null)
            return "n/a";

        if (previous.Value == 0)
            return current == 0 ? "0%" : "n/a";

        double delta = ((current - previous.Value) / previous.Value) * 100;
        return delta.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) + "%";
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner [language]");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner --filter <pattern>");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner --dry-run");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner --plot");
        Console.WriteLine("  FastData.BenchmarkHarness.Runner --individual-plot");
        Console.WriteLine();
        Console.WriteLine("Filters use BenchmarkDotNet-style wildcards and match Language.Identifier names, for example:");
        Console.WriteLine("  --filter \"*HashTable*\"");
        Console.WriteLine("  --filter \"CSharp.*Int32*\"");
        Console.WriteLine("  --dry-run --filter \"Rust.*Pgm*\"");
        Console.WriteLine("  --plot --filter \"CSharp.*Pgm*\"");
        Console.WriteLine("  --individual-plot --filter \"CSharp.*Pgm*\"");
        Console.WriteLine("  CSharp");
    }

    private static string FormatResult(double value) => value.ToString("0.#################", CultureInfo.InvariantCulture);

    private sealed record BenchmarkHistory(string Name, BenchmarkResultEntry[] Entries);

    private sealed record BenchmarkResultEntry(string Name, double Min, double Median, double Max, double Avg, DateTimeOffset TimestampUtc);
}