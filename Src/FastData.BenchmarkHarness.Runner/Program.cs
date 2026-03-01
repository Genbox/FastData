using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.BenchmarkHarness.Runner;

internal static class Program
{
    private const bool UseBencher = false;
    private const bool UseShell = true;

    private static readonly BenchmarkBase[] Harnesses =
    [
        CSharpBenchmark.Instance,
        CPlusPlusBenchmark.Instance,
        RustBenchmark.Instance
    ];

    private static void Main()
    {
        Parallel.ForEach(Harnesses, RunHarness);
    }

    private static void RunHarness(BenchmarkBase harness)
    {
        BenchmarkSuite suite = harness.CreateFiles(TestVectorHelper.GetBenchmarkData());
        WriteSuite(harness.RootDir, suite);

        Console.WriteLine($"Executing {harness.Name}");
        harness.Run(suite, UseBencher, UseShell);
    }

    private static void WriteSuite(string rootDir, BenchmarkSuite suite)
    {
        string entryPath = Path.Combine(rootDir, suite.EntryFilename);
        FileHelper.TryWriteFile(entryPath, suite.EntrySource);

        foreach (BenchmarkFile file in suite.AdditionalFiles)
        {
            string filePath = Path.Combine(rootDir, file.Filename);
            string? fileDir = Path.GetDirectoryName(filePath);

            if (fileDir != null && !Directory.Exists(fileDir))
                Directory.CreateDirectory(fileDir);

            FileHelper.TryWriteFile(filePath, file.Source);
        }
    }
}