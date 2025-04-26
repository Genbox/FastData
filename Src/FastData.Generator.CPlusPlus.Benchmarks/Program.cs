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
        CPlusPlusCompiler compiler = new CPlusPlusCompiler(true);

        foreach ((StructureType type, object[] data) in TestVectorHelper.GetBenchmarkVectors())
        {
            if (!TestVectorHelper.TryGenerate(id => new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig(id)), type, data, out GeneratorSpec spec))
                throw new InvalidOperationException("Unable to build " + type);

            string executable = compiler.Compile(spec.Identifier,
                $$"""
                  #include <benchmark/benchmark.h>

                  {{spec.Source}}

                  using namespace benchmark;

                  static void BM_{{spec.Identifier}}(State& state)
                  {
                      for (auto _ : state)
                      {
                          DoNotOptimize({{spec.Identifier}}::contains({{ToValueLabel(data[0])}}));
                      }
                  }

                  BENCHMARK(BM_{{spec.Identifier}});

                  int main(int argc, char* argv[])
                  {
                      Initialize(&argc, argv);
                      RunSpecifiedBenchmarks();
                      return 0;
                  }
                  """);

            int res = TestHelper.RunProcess(executable);

            if (res != 0)
                throw new InvalidOperationException("Failed to run executable. Return code: " + res);
        }
    }
}