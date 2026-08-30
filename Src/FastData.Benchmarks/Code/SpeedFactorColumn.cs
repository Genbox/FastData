using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Genbox.FastData.Benchmarks.Code;

internal class SpeedFactorColumn : IColumn
{
    public string Id => nameof(SpeedFactorColumn);
    public string ColumnName => "Factor";

    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "How many times faster compared to the baseline";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        BenchmarkCase? baseline = FindBaseline(summary, benchmarkCase);

        if (baseline == null || baseline == benchmarkCase)
            return "-";

        Statistics? baselineStats = FindStatistics(summary, baseline);
        Statistics? currentStats = FindStatistics(summary, benchmarkCase);

        if (baselineStats == null || currentStats == null)
            return "?";

        double ratio = baselineStats.Mean / currentStats.Mean;
        return ratio.ToString("0.00", NumberFormatInfo.InvariantInfo) + "x";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

    private static BenchmarkCase? FindBaseline(Summary summary, BenchmarkCase benchmarkCase)
    {
        foreach (BenchmarkCase candidate in summary.BenchmarksCases)
        {
            if (candidate.Descriptor.Baseline && HasSharedCategory(candidate, benchmarkCase))
                return candidate;
        }

        return null;
    }

    private static bool HasSharedCategory(BenchmarkCase left, BenchmarkCase right)
    {
        foreach (string leftCategory in left.Descriptor.Categories)
        {
            foreach (string rightCategory in right.Descriptor.Categories)
            {
                if (string.Equals(leftCategory, rightCategory, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static Statistics? FindStatistics(Summary summary, BenchmarkCase benchmarkCase)
    {
        foreach (BenchmarkReport report in summary.Reports)
        {
            if (ReferenceEquals(report.BenchmarkCase, benchmarkCase))
                return report.ResultStatistics;
        }

        return null;
    }
}