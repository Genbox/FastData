using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Generator.CPlusPlus.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");
        TestHelper.CreateOrEmptyDirectory(rootDir);

        GccCompiler compiler = new GccCompiler(true, rootDir, true);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("""
                      #include <benchmark/benchmark.h>
                      using namespace benchmark;
                      """);

        foreach (ITestData data in TestVectorHelper.GetBenchmarkData())
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
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        DoNotOptimize({identifier}::contains({data.GetValueLabel(map)}));");
        }

        return sb.ToString();
    }
}