using System.Globalization;
using ConsolePlot;
using ConsolePlot.Drawing.Tools;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;
using Genbox.FastData.BenchmarkHarness.Runner.Results;

namespace Genbox.FastData.BenchmarkHarness.Runner.Plotting;

internal sealed class BenchmarkPlotter(PlotSettings settings)
{
    private static readonly (ConsoleColor PlotColor, string Style)[] PlotColors =
    [
        (ConsoleColor.Cyan, "cyan"),
        (ConsoleColor.Yellow, "yellow"),
        (ConsoleColor.Green, "green"),
        (ConsoleColor.Magenta, "magenta"),
        (ConsoleColor.Blue, "blue"),
        (ConsoleColor.Red, "red"),
        (ConsoleColor.White, "white"),
        (ConsoleColor.DarkCyan, "darkcyan"),
        (ConsoleColor.DarkYellow, "olive"),
        (ConsoleColor.DarkGreen, "darkgreen"),
        (ConsoleColor.DarkMagenta, "purple"),
        (ConsoleColor.DarkBlue, "navy"),
        (ConsoleColor.DarkRed, "maroon"),
        (ConsoleColor.Gray, "grey")
    ];

    public void PlotCombined(IReadOnlyList<BenchmarkHistory> histories)
    {
        BenchmarkConsole.WriteHeading("Median benchmark history");

        Plot plot = new Plot(GetPlotWidth(), settings.Height);

        for (int i = 0; i < histories.Count; i++)
        {
            BenchmarkHistory history = histories[i];
            (PointPen pen, string style) = GetPlotPen(i);
            AddMedianSeries(plot, history, pen);
            BenchmarkConsole.WriteColoredLinePrefix(i + 1, style, history.Name, $" ({history.Entries.Length} data points)");
        }

        DrawPlot(plot, histories.SelectMany(x => x.Entries), histories.Max(x => x.Entries.Length));
    }

    public void PlotIndividual(IEnumerable<BenchmarkHistory> histories)
    {
        foreach (BenchmarkHistory history in histories)
            PlotHistory(history);
    }

    private void PlotHistory(BenchmarkHistory history)
    {
        BenchmarkConsole.WriteHeading(history.Name);

        Plot plot = new Plot(GetPlotWidth(), settings.Height);
        AddMedianSeries(plot, history, GetPlotPen(0).Pen);
        DrawPlot(plot, history.Entries, history.Entries.Length);
    }

    private static void AddMedianSeries(Plot plot, BenchmarkHistory history, PointPen pen)
    {
        double[] xs = Enumerable.Range(1, history.Entries.Length).Select(x => (double)x).ToArray();
        double[] medians = history.Entries.Select(x => x.Median).ToArray();
        plot.AddSeries(xs, medians, pen);
    }

    private static (PointPen Pen, string Style) GetPlotPen(int index)
    {
        (ConsoleColor plotColor, string style) = PlotColors[index % PlotColors.Length];
        return (new PointPen(SystemPointBrushes.Braille, plotColor), style);
    }

    private void DrawPlot(Plot plot, IEnumerable<BenchmarkResultEntry> entries, int maxDataPointCount)
    {
        BenchmarkResultEntry[] entryArray = entries.ToArray();
        DateTimeOffset minTimestamp = entryArray.Min(x => x.TimestampUtc);
        DateTimeOffset maxTimestamp = entryArray.Max(x => x.TimestampUtc);

        BenchmarkConsole.WriteInfo("X axis", "result number");
        BenchmarkConsole.WriteInfo("Y axis", "median");
        BenchmarkConsole.WriteInfo("Timestamp range", $"{FormatTimestamp(minTimestamp)} to {FormatTimestamp(maxTimestamp)}");
        plot.Axis.IsVisible = true;
        plot.Grid.IsVisible = true;
        plot.Ticks.IsVisible = true;
        plot.Ticks.DesiredXStep = GetDesiredXStep(maxDataPointCount);
        plot.Ticks.Labels.IsVisible = true;
        plot.Ticks.Labels.Format = "0";
        plot.Draw();
        plot.Render();
    }

    private int GetDesiredXStep(int maxDataPointCount)
    {
        int targetTickCount = Math.Clamp(maxDataPointCount, 2, settings.MaxXTickLabels);
        return Math.Max(8, GetPlotWidth() / targetTickCount);
    }

    private int GetPlotWidth()
    {
        if (settings.Width > 0)
            return settings.Width;

        if (Console.IsOutputRedirected)
            return 100;

        return Math.Clamp(Console.WindowWidth - 1, 60, 140);
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.UtcDateTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss 'UTC'", CultureInfo.InvariantCulture);
}