using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Shared;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.Generator.CPlusPlus.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CPlusPlus.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");
        Directory.CreateDirectory(rootDir);

        CPlusPlusCompiler compiler = new CPlusPlusCompiler(true, rootDir);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("""
                      #include <benchmark/benchmark.h>
                      using namespace benchmark;
                      """);

        foreach (ITestData data in TestVectorHelper.GetBenchmarkVectors())
        {
            if (!TestVectorHelper.TryGenerate(id => new CPlusPlusCodeGenerator(new CPlusPlusCodeGeneratorConfig(id)), data, out GeneratorSpec spec))
                throw new InvalidOperationException("Unable to build " + data.StructureType);

            sb.AppendLine(CultureInfo.InvariantCulture,
                $$"""
                  {{spec.Source}}

                  static void CPlusPlus_{{spec.Identifier}}(State& state)
                  {
                      for (auto _ : state)
                      {
                  {{PrintQueries(data.Items, spec.Identifier)}}
                      }
                  }

                  BENCHMARK(CPlusPlus_{{spec.Identifier}});
                  """);
        }

        sb.AppendLine("BENCHMARK_MAIN();");

        string executable = compiler.Compile("all_benchmarks", sb.ToString());
        BenchmarkHelper.RunBenchmark(executable, "--benchmark_format=json", rootDir, "--adapter cpp_google --testbed CPlusPlus");
    }

    private static string PrintQueries(object[] data, string identifier)
    {
        Random rng = new Random(42);
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
            sb.AppendLine(CultureInfo.InvariantCulture, $"        DoNotOptimize({identifier}::contains({ToValueLabel(data[rng.Next(0, data.Length)])}));");

        return sb.ToString();
    }
}