using System.Globalization;
using Genbox.FastData.BenchmarkHarness.Runner.Results;
using Spectre.Console;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class BenchmarkConsole
{
    private static readonly IAnsiConsole ErrorConsole = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) });

    public static void WriteBenchmarkSetup(params (string Label, string Value)[] rows)
    {
        Table table = new Table()
                      .Border(TableBorder.Rounded)
                      .HideHeaders();

        table.AddColumn(new TableColumn(string.Empty).NoWrap());
        table.AddColumn(new TableColumn(string.Empty));

        foreach ((string label, string value) in rows)
            table.AddRow($"[grey]{Markup.Escape(label)}[/]", Markup.Escape(value));

        AnsiConsole.Write(table);
    }

    public static void WriteBenchmarkResult(BenchmarkResultLine result)
    {
        Table table = new Table()
                      .Border(TableBorder.None)
                      .HideHeaders()
                      .NoSafeBorder();

        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(10));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(30));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(23));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(23));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(29));
        table.AddColumn(new TableColumn(string.Empty).NoWrap());

        table.AddRow(
            Markup.Escape(result.HarnessName),
            Markup.Escape(result.DataIdentifier),
            FormatMetric("min", result.Min),
            FormatMetric("max", result.Max),
            FormatDeltaMetric("mid", result.Median, result.MedianDelta),
            FormatDeltaMetric("avg", result.Avg, result.AvgDelta));

        AnsiConsole.Write(table);
    }

    public static void WriteError(string message) => ErrorConsole.MarkupLine(CultureInfo.InvariantCulture, "[red]{0}[/]", Markup.Escape(message));

    public static void WriteHeading(string text)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule(Markup.Escape(text)).LeftJustified());
    }

    public static void WriteInfo(string label, string value) => AnsiConsole.MarkupLine(CultureInfo.InvariantCulture, "[grey]{0}:[/] {1}", Markup.Escape(label), Markup.Escape(value));

    public static void WriteColoredLinePrefix(int index, string style, string text, string suffix)
    {
        AnsiConsole.Markup(CultureInfo.InvariantCulture, "{0,2}. ", index);
        AnsiConsole.Markup(CultureInfo.InvariantCulture, "[{0}]{1}[/]", style, Markup.Escape(text));
        AnsiConsole.MarkupLine(Markup.Escape(suffix));
    }

    private static string FormatMetric(string label, string value) => $"[grey]{label}:[/] {Markup.Escape(value)}";

    private static string FormatDeltaMetric(string label, string value, BenchmarkResultDelta delta) =>
        FormatMetric(label, value) + " (" + FormatDelta(delta) + ")";

    private static string FormatDelta(BenchmarkResultDelta delta) =>
        delta.Warning ? $"[red]{Markup.Escape(delta.Text)}[/]" : Markup.Escape(delta.Text);
}