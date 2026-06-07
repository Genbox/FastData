using System.Globalization;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class Program
{
    private static readonly Func<DockerManager, BenchmarkBase>[] HarnessFactories =
    [
        x => new CSharpBenchmark(x),
        x => new CPlusPlusBenchmark(x),
        x => new RustBenchmark(x)
    ];

    private static async Task Main()
    {
        CpuSelection? cpuSelection = CpuSelector.TryGetSelection();

        if (cpuSelection is null)
            Console.WriteLine("Benchmark CPU selection: using default CPU set 0.");
        else
            Console.WriteLine($"Benchmark CPU selection: {cpuSelection.CpuSet} (core {cpuSelection.PhysicalCoreIndex}, siblings {cpuSelection.Siblings}, logical {cpuSelection.LogicalProcessorCount}, cores {cpuSelection.PhysicalCoreCount}).");

        string cpuSet = cpuSelection?.CpuSet ?? "0";

        foreach (Func<DockerManager, BenchmarkBase> factory in HarnessFactories)
            await RunHarnessAsync(factory, cpuSet, CancellationToken.None);
    }

    private static async ValueTask RunHarnessAsync(Func<DockerManager, BenchmarkBase> harnessFactory, string cpuSet, CancellationToken cancellationToken)
    {
        await using DockerManager dockerManager = new DockerManager(cpuSet: cpuSet);
        BenchmarkBase harness = harnessFactory(dockerManager);

        foreach (ITestData data in TestVectorHelper.GetBenchmarkData())
        {
            BenchmarkResult result = await harness.RunAsync(data, cancellationToken);
            string min = FormatResult(result.Min);
            string median = FormatResult(result.Median);
            string max = FormatResult(result.Max);
            Console.WriteLine($"{harness.Name,-10} {data.Identifier,-30} min={min,-18} median={median,-18} max={max}");
        }
    }

    private static string FormatResult(double value) => value.ToString("0.#################", CultureInfo.InvariantCulture);
}