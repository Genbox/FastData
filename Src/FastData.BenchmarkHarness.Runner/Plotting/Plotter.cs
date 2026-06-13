using System.Globalization;
using ConsolePlot;
using ConsolePlot.Drawing.Tools;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;
using Genbox.FastData.BenchmarkHarness.Runner.Results;

namespace Genbox.FastData.BenchmarkHarness.Runner.Plotting;

internal sealed class Plotter(PlotSettings settings)
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

    public void PlotCombined(IReadOnlyList<History> histories)
    {
        if (histories.Count == 0)
            return;

        ConsoleOutput.WriteHeading("Median benchmark history");

        int plotWidth = GetPlotWidth();
        Plot plot = new Plot(plotWidth, settings.Height);

        for (int i = 0; i < histories.Count; i++)
        {
            History history = histories[i];
            (PointPen pen, string style) = GetPlotPen(i, SystemPointBrushes.Braille);
            AddMedianSeries(plot, history, pen);
            ConsoleOutput.WriteColoredLinePrefix(i + 1, style, history.Name, $" ({history.Entries.Length} data points)");
        }

        DrawPlot(plot, histories.SelectMany(x => x.Entries), histories.Max(x => x.Entries.Length), plotWidth);
    }

    public void PlotIndividual(IEnumerable<History> histories)
    {
        foreach (History history in histories)
            PlotHistory(history);
    }

    private void PlotHistory(History history)
    {
        ConsoleOutput.WriteHeading(history.Name);

        if (history.Entries.Length == 1)
        {
            ResultEntry entry = history.Entries[0];
            ConsoleOutput.WriteInfo("Median", entry.Median.ToString("0.####", CultureInfo.InvariantCulture));
            ConsoleOutput.WriteInfo("Timestamp", FormatTimestamp(entry.TimestampUtc));
            ConsoleOutput.WriteInfo("History", "only one data point; at least two are needed to plot a trend");
            return;
        }

        int plotWidth = GetPlotWidth();
        Plot plot = new Plot(plotWidth, settings.Height);
        AddMedianSeries(plot, history, GetPlotPen(0, SystemPointBrushes.Braille).Pen);
        DrawPlot(plot, history.Entries, history.Entries.Length, plotWidth);
    }

    private static void AddMedianSeries(Plot plot, History history, PointPen pen)
    {
        if (history.Entries.Length == 1)
        {
            double median = history.Entries[0].Median;
            plot.AddSeries([1, 2], [median, median], pen);
            return;
        }

        double[] xs = Enumerable.Range(1, history.Entries.Length).Select(x => (double)x).ToArray();
        double[] medians = history.Entries.Select(x => x.Median).ToArray();
        plot.AddSeries(xs, medians, pen);
    }

    private static (PointPen Pen, string Style) GetPlotPen(int index, IPointBrush brush)
    {
        (ConsoleColor plotColor, string style) = PlotColors[index % PlotColors.Length];
        return (new PointPen(brush, plotColor), style);
    }

    private void DrawPlot(Plot plot, IEnumerable<ResultEntry> entries, int maxDataPointCount, int plotWidth)
    {
        ResultEntry[] entryArray = entries.ToArray();

        if (entryArray.Length == 0)
            return;

        DateTimeOffset minTimestamp = entryArray.Min(x => x.TimestampUtc);
        DateTimeOffset maxTimestamp = entryArray.Max(x => x.TimestampUtc);

        ConsoleOutput.WriteInfo("X axis", "result number");
        ConsoleOutput.WriteInfo("Y axis", "median");
        ConsoleOutput.WriteInfo("Timestamp range", $"{FormatTimestamp(minTimestamp)} to {FormatTimestamp(maxTimestamp)}");
        plot.Axis.IsVisible = true;
        plot.Grid.IsVisible = true;
        plot.Ticks.IsVisible = true;
        plot.Ticks.DesiredXStep = GetDesiredXStep(maxDataPointCount, plotWidth);
        plot.Ticks.Labels.IsVisible = true;
        plot.Ticks.Labels.AttachToAxis = false;
        plot.Ticks.Labels.Format = "0";
        plot.Draw();
        plot.Render();
    }

    private int GetDesiredXStep(int maxDataPointCount, int plotWidth)
    {
        int targetTickCount = Math.Clamp(maxDataPointCount, 2, settings.MaxXTickLabels);
        return Math.Max(8, plotWidth / targetTickCount);
    }

    private int GetPlotWidth()
    {
        if (settings.Width > 0)
            return settings.Width;

        if (Console.IsOutputRedirected)
            return 100;

        try
        {
            return Math.Clamp(Console.WindowWidth - 1, 60, 140);
        }
        catch (IOException)
        {
            return 100;
        }
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.UtcDateTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss 'UTC'", CultureInfo.InvariantCulture);
}