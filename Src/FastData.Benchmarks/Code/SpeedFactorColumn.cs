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
        string[] categories = benchmarkCase.Descriptor.Categories.ToArray();

        BenchmarkCase? baseline = summary.BenchmarksCases.FirstOrDefault(b => b.Descriptor.Baseline && b.Descriptor.Categories.Intersect(categories).Any());

        if (baseline == null || baseline == benchmarkCase)
            return "-";

        Statistics? baselineStats = summary[baseline]?.ResultStatistics;
        Statistics? currentStats = summary[benchmarkCase]?.ResultStatistics;

        if (baselineStats == null || currentStats == null)
            return "?";

        double ratio = baselineStats.Mean / currentStats.Mean;
        return ratio.ToString("0.00", NumberFormatInfo.InvariantInfo) + "x";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
}