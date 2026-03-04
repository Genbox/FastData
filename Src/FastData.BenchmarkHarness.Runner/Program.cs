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
        foreach (Func<DockerManager, BenchmarkBase> factory in HarnessFactories)
            await RunHarnessAsync(factory, CancellationToken.None);
    }

    private static async ValueTask RunHarnessAsync(Func<DockerManager, BenchmarkBase> harnessFactory, CancellationToken cancellationToken)
    {
        await using DockerManager dockerManager = new DockerManager();
        BenchmarkBase harness = harnessFactory(dockerManager);

        foreach (ITestData data in TestVectorHelper.GetBenchmarkData())
        {
            double res = await harness.RunAsync(data, cancellationToken);
            string value = res.ToString("0.#################", CultureInfo.InvariantCulture);
            Console.WriteLine($"{harness.Name,-10} {data.Identifier,-30} {value}");
        }
    }
}