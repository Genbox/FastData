using System.Globalization;
using System.Text;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.Shared;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.CPlusPlus.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");
        TestHelper.CreateOrEmptyDirectory(rootDir);

        CPlusPlusCompiler compiler = new CPlusPlusCompiler(true, rootDir);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("""
                      #include <benchmark/benchmark.h>
                      using namespace benchmark;
                      """);

        foreach (ITestData data in TestVectorHelper.GetBenchmarkVectors())
        {
            data.Generate(id => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)), out GeneratorSpec spec);

            sb.AppendLine(CultureInfo.InvariantCulture,
                $$"""
                  {{spec.Source}}

                  static void CPlusPlus_{{spec.Identifier}}(State& state)
                  {
                      for (auto _ : state)
                      {
                  {{PrintQueries(data, spec.Identifier)}}
                      }
                  }

                  BENCHMARK(CPlusPlus_{{spec.Identifier}});
                  """);
        }

        sb.AppendLine("BENCHMARK_MAIN();");

        string executable = compiler.Compile("all_benchmarks", sb.ToString());
        BenchmarkHelper.RunBenchmark(executable, "--benchmark_format=json", rootDir, "--adapter cpp_google --testbed CPlusPlus");
    }

    private static string PrintQueries(ITestData data, string identifier)
    {
        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeHelper helper = new TypeHelper(new TypeMap(langDef.TypeDefinitions));

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
            sb.AppendLine(CultureInfo.InvariantCulture, $"        DoNotOptimize({identifier}::contains({data.GetValueLabel(helper)}));");

        return sb.ToString();
    }
}