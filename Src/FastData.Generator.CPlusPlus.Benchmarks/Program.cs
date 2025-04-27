using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Shared;
using Genbox.FastData.Generator.Helpers;
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

        foreach ((StructureType type, object[] data) in TestVectorHelper.GetBenchmarkVectors())
        {
            if (!TestVectorHelper.TryGenerate(id => new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig(id)), type, data, out GeneratorSpec spec))
                throw new InvalidOperationException("Unable to build " + type);

            sb.AppendLine(CultureInfo.InvariantCulture,
                $$"""
                  {{spec.Source}}

                  static void BM_{{spec.Identifier}}(State& state)
                  {
                      for (auto _ : state)
                      {
                          DoNotOptimize({{spec.Identifier}}::contains({{ToValueLabel(data[0])}}));
                      }
                  }

                  BENCHMARK(BM_{{spec.Identifier}});
                  """);

        }
        sb.AppendLine("BENCHMARK_MAIN();");

        string executable = compiler.Compile("all_benchmarks", sb.ToString());
        BenchmarkHelper.RunBenchmark(executable, "--benchmark_format=json", "cpp_google", rootDir);
    }
}