using System.Text.RegularExpressions;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;
using Genbox.FastData.BenchmarkHarness.Runner.Results;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner.Catalog;

internal sealed class BenchmarkCatalog
{
    private readonly Descriptor[] _descriptors =
    [
        new Descriptor("CSharp", x => new CSharpBenchmark(x)),
        new Descriptor("CPlusPlus", x => new CPlusPlusBenchmark(x)),
        new Descriptor("Rust", x => new RustBenchmark(x))
    ];

    public Selection[] Select(ITestData[] benchmarkData, Settings settings)
    {
        List<Selection> selections = [];

        foreach (Descriptor descriptor in GetDescriptors(settings))
        {
            ITestData[] data = GetMatches(benchmarkData, settings).Where(x => x.Descriptor == descriptor).Select(x => x.Data).ToArray();
            if (data.Length > 0)
                selections.Add(new Selection(descriptor.Factory, data));
        }

        return selections.ToArray();
    }

    public string[] GetMatchingNames(ITestData[] benchmarkData, Settings settings) => GetMatches(benchmarkData, settings).Select(x => x.Name).ToArray();

    public IEnumerable<History> GetHistories(ITestData[] benchmarkData, Settings settings, ResultStore resultStore)
    {
        foreach ((Descriptor _, ITestData _, string name) in GetMatches(benchmarkData, settings))
        {
            ResultEntry[] entries = resultStore.ReadHistory(name);
            if (entries.Length > 0)
                yield return new History(name, entries);
        }
    }

    public string[] LanguageNames => _descriptors.Select(x => x.Name).ToArray();

    public static string GetBenchmarkName(string harnessName, ITestData data) => harnessName + "." + data.Identifier;

    private IEnumerable<(Descriptor Descriptor, ITestData Data, string Name)> GetMatches(ITestData[] benchmarkData, Settings settings)
    {
        Regex[] compiledFilters = CompileFilters(settings.Filters);

        foreach (Descriptor descriptor in GetDescriptors(settings))
        {
            foreach (ITestData data in benchmarkData)
            {
                string name = GetBenchmarkName(descriptor.Name, data);
                if (MatchesAny(name, compiledFilters))
                    yield return (descriptor, data, name);
            }
        }
    }

    private IEnumerable<Descriptor> GetDescriptors(Settings settings)
    {
        if (settings.Languages.Length == 0)
            return _descriptors;

        return _descriptors.Where(x => settings.Languages.Any(y => string.Equals(x.Name, y, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool MatchesAny(string benchmarkName, Regex[] compiledFilters)
    {
        foreach (Regex filter in compiledFilters)
        {
            if (filter.IsMatch(benchmarkName))
                return true;
        }

        return false;
    }

    private static Regex[] CompileFilters(string[] filters) => filters.Select(CompileFilter).ToArray();

    private static Regex CompileFilter(string filter)
    {
        string pattern = filter.Replace("*", ".*", StringComparison.Ordinal);
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    private sealed record Descriptor(string Name, Func<DockerManager, BenchmarkBase> Factory);
}