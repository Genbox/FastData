using System.Globalization;
using Spectre.Console;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class ConsoleOutput
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

    public static void WriteBenchmarkResult(ResultLine result, double warningThreshold)
    {
        Table table = new Table()
                      .Border(TableBorder.None)
                      .HideHeaders()
                      .NoSafeBorder();

        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(10));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(45));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(14));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(14));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(22));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(22));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(14));
        table.AddColumn(new TableColumn(string.Empty).NoWrap().Width(14));
        table.AddColumn(new TableColumn(string.Empty).NoWrap());

        table.AddRow(
            Markup.Escape(result.HarnessName),
            Markup.Escape(result.DataIdentifier),
            FormatMetric("min", result.Min),
            FormatMetric("max", result.Max),
            FormatDeltaMetric("mid", result.Median, result.PreviousMedian, warningThreshold),
            FormatDeltaMetric("avg", result.Avg, result.PreviousAvg, warningThreshold),
            FormatMetric("err", result.Error),
            FormatMetric("std", result.StdDev),
            FormatMetric("out", $"{result.Outliers.ToString(CultureInfo.InvariantCulture)}/{result.Samples.ToString(CultureInfo.InvariantCulture)}"));

        AnsiConsole.Write(table);
    }

    public static void WriteError(string message) => ErrorConsole.MarkupLine(CultureInfo.InvariantCulture, "[red]{0}[/]", Markup.Escape(message));

    public static void WriteDebug(string message) => AnsiConsole.MarkupLine(CultureInfo.InvariantCulture, "[grey]Debug:[/] {0}", Markup.Escape(message));

    public static void WriteHeading(string text)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule(Markup.Escape(text)).LeftJustified());
    }

    public static void WriteInfo(string label, string value) => AnsiConsole.MarkupLine(CultureInfo.InvariantCulture, "[grey]{0}:[/] {1}", Markup.Escape(label), Markup.Escape(value));

    public static void WriteWarning(string message) => AnsiConsole.MarkupLine(CultureInfo.InvariantCulture, "[yellow]Warning:[/] {0}", Markup.Escape(message));

    public static void WriteColoredLinePrefix(int index, string style, string text, string suffix)
    {
        AnsiConsole.Markup(CultureInfo.InvariantCulture, "{0,2}. ", index);
        AnsiConsole.Markup(CultureInfo.InvariantCulture, "[{0}]{1}[/]", style, Markup.Escape(text));
        AnsiConsole.MarkupLine(Markup.Escape(suffix));
    }

    private static string FormatMetric(string label, double value) => $"[grey]{label}:[/] {value.ToString("0.0000", CultureInfo.InvariantCulture)}";

    private static string FormatMetric(string label, string value) => $"[grey]{label}:[/] {value}";

    private static string FormatDeltaMetric(string label, double value, double? previous, double warningThreshold) => $"{FormatMetric(label, value)} ({FormatDelta(value, previous, warningThreshold)})";

    private static string FormatDelta(double current, double? previous, double warningThreshold)
    {
        if (previous is null)
            return "n/a";

        if (previous.Value == 0)
            return current == 0 ? "0%" : "n/a";

        double delta = ((current - previous.Value) / previous.Value) * 100;
        string text = delta.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + "%";
        string? style = Math.Abs(delta) < warningThreshold ? null :
            delta < 0 ? "green" : "red";
        return style == null ? text : $"[{style}]{text}[/]";
    }
}