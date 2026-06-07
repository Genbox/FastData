using Genbox.FastData.BenchmarkHarness.Runner.Configuration;
using Genbox.FastData.BenchmarkHarness.Runner.Filtering;
using Genbox.FastData.BenchmarkHarness.Runner.Results;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner.Catalog;

internal sealed class BenchmarkCatalog
{
    private readonly BenchmarkDescriptor[] _descriptors =
    [
        new BenchmarkDescriptor("CSharp", x => new CSharpBenchmark(x)),
        new BenchmarkDescriptor("CPlusPlus", x => new CPlusPlusBenchmark(x)),
        new BenchmarkDescriptor("Rust", x => new RustBenchmark(x))
    ];

    public ITestData[] CreateBenchmarkData(BenchmarkSettings settings) => TestVectorHelper.GetBenchmarkData(
        settings.WarmupCount,
        settings.SampleCount,
        settings.WorkIterations,
        settings.QueryCount,
        settings.BenchmarkSize,
        settings.KeyLengthBenchmarkSize).ToArray();

    public BenchmarkSelection[] Select(ITestData[] benchmarkData, BenchmarkSettings settings)
    {
        List<BenchmarkSelection> selections = [];

        foreach (BenchmarkDescriptor descriptor in GetDescriptors(settings))
        {
            ITestData[] data = benchmarkData.Where(x => BenchmarkFilter.MatchesAny(GetBenchmarkName(descriptor.Name, x), settings.Filters)).ToArray();
            if (data.Length > 0)
                selections.Add(new BenchmarkSelection(descriptor.Name, descriptor.Factory, data));
        }

        return selections.ToArray();
    }

    public IEnumerable<string> GetSelectedNames(ITestData[] benchmarkData, BenchmarkSettings settings)
    {
        foreach (BenchmarkDescriptor descriptor in GetDescriptors(settings))
        {
            foreach (ITestData data in benchmarkData)
            {
                string name = GetBenchmarkName(descriptor.Name, data);
                if (BenchmarkFilter.MatchesAny(name, settings.Filters))
                    yield return name;
            }
        }
    }

    public IEnumerable<BenchmarkHistory> GetHistories(ITestData[] benchmarkData, BenchmarkSettings settings, BenchmarkResultStore resultStore)
    {
        foreach (BenchmarkDescriptor descriptor in GetDescriptors(settings))
        {
            foreach (ITestData data in benchmarkData)
            {
                string benchmarkName = GetBenchmarkName(descriptor.Name, data);
                if (!BenchmarkFilter.MatchesAny(benchmarkName, settings.Filters))
                    continue;

                BenchmarkResultEntry[] entries = resultStore.ReadHistory(benchmarkName);
                if (entries.Length > 0)
                    yield return new BenchmarkHistory(benchmarkName, entries);
            }
        }
    }

    public static string GetBenchmarkName(string harnessName, ITestData data) => harnessName + "." + data.Identifier;

    private IEnumerable<BenchmarkDescriptor> GetDescriptors(BenchmarkSettings settings)
    {
        if (settings.Languages.Length == 0)
            return _descriptors;

        return _descriptors.Where(x => settings.Languages.Any(y => string.Equals(x.Name, y, StringComparison.OrdinalIgnoreCase)));
    }

    public bool IsLanguageName(string value) => _descriptors.Any(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));
}