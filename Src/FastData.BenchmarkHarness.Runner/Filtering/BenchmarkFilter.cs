using System.Text.RegularExpressions;

namespace Genbox.FastData.BenchmarkHarness.Runner.Filtering;

internal static class BenchmarkFilter
{
    public static bool MatchesAny(string benchmarkName, IEnumerable<string> filters) => filters.Any(x => Matches(benchmarkName, x));

    private static bool Matches(string benchmarkName, string filter)
    {
        string pattern = "^" + Regex.Escape(filter).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(benchmarkName, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}