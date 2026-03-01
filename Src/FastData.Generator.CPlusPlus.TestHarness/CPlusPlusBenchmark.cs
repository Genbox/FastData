using System.Globalization;
using System.Text;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusBenchmark : BenchmarkBase<CPlusPlusBootstrap>
{
    private CPlusPlusBenchmark() : base(new CPlusPlusBootstrap(HarnessType.Benchmark)) {}

    public static CPlusPlusBenchmark Instance { get; } = new CPlusPlusBenchmark();

    public override BenchmarkSuite CreateFiles(IEnumerable<ITestData> data)
    {
        List<BenchmarkFile> files = new List<BenchmarkFile>();

        foreach (ITestData item in data)
        {
            item.Generate(Bootstrap.GeneratorFactory, out GeneratorSpec spec);

            string filename = "bench_" + spec.Identifier + ".cpp";
            files.Add(new BenchmarkFile(filename, $$"""
                                                    #include <benchmark/benchmark.h>
                                                    using namespace benchmark;

                                                    {{spec.Source}}

                                                    static void CPlusPlus_{{spec.Identifier}}(State& state)
                                                    {
                                                        for (auto _ : state)
                                                        {
                                                    {{RenderBenchmarkQueries(item, spec.Identifier)}}
                                                        }
                                                    }

                                                    BENCHMARK(CPlusPlus_{{spec.Identifier}});
                                                    """));
        }

        files.Add(new BenchmarkFile("main.cpp", """
                                                #include <benchmark/benchmark.h>

                                                BENCHMARK_MAIN();
                                                """));

        return new BenchmarkSuite("CMakeLists.txt", BuildCMakeLists(files.Select(x => x.Filename)), files);
    }

    public override void Run(BenchmarkSuite suite, bool useBencher, bool useShell)
    {
        ProcessResult configureResult = ProcessHelper.RunProcess("cmake", "-S . -B build", Bootstrap.RootDir, 100_000);

        if (configureResult.ExitCode != 0)
            throw new InvalidOperationException($"Failed to configure CMake. Exit code: {configureResult.ExitCode}\nSTDOUT:\n{configureResult.StandardOutput}\nSTDERR:\n{configureResult.StandardError}");

        ProcessResult buildResult = ProcessHelper.RunProcess("cmake", "--build build --config Release", Bootstrap.RootDir, 100_000);

        if (buildResult.ExitCode != 0)
            throw new InvalidOperationException($"Failed to build benchmarks. Exit code: {buildResult.ExitCode}\nSTDOUT:\n{buildResult.StandardOutput}\nSTDERR:\n{buildResult.StandardError}");

        string executable = Path.Combine(Bootstrap.RootDir, "build", "out", "all_benchmarks.exe");

        string args = useShell ? "--benchmark_min_time=5s" : "--benchmark_min_time=5s --benchmark_format=json";
        BenchmarkHelper.RunBenchmark(executable, args, Bootstrap.RootDir, "--adapter cpp_google --testbed CPlusPlus", useBencher, useShell);
    }

    private string RenderBenchmarkQueries(ITestData data, string identifier)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
            sb.AppendLine(CultureInfo.InvariantCulture, $"        DoNotOptimize({identifier}::contains({data.GetValueLabel(Bootstrap.Map)}));");

        return sb.ToString();
    }

    private static string BuildCMakeLists(IEnumerable<string> sources) =>
        $$"""
          cmake_minimum_required(VERSION 3.20)
          project(fast_data_benchmarks LANGUAGES CXX)
          set(CMAKE_CXX_STANDARD 17)
          set(CMAKE_CXX_STANDARD_REQUIRED ON)
          set(CMAKE_CXX_EXTENSIONS OFF)

          set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out")
          set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_DEBUG "${CMAKE_BINARY_DIR}/out")
          set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_BINARY_DIR}/out")

          add_compile_options(
            $<$<CONFIG:Release>:/O2>
          )

          set(SOURCES
          {{string.Join(Environment.NewLine, sources.Select(source => $"  {source}"))}}
          )

          add_executable(all_benchmarks ${SOURCES})
          include(FetchContent)
          set(BENCHMARK_ENABLE_TESTING OFF CACHE BOOL "" FORCE)
          set(BENCHMARK_ENABLE_GTEST_TESTS OFF CACHE BOOL "" FORCE)

          FetchContent_Declare(
            benchmark
            URL https://github.com/google/benchmark/archive/refs/tags/v1.9.5.zip
          )
          FetchContent_MakeAvailable(benchmark)
          target_link_libraries(all_benchmarks PRIVATE benchmark::benchmark)
          """;
}