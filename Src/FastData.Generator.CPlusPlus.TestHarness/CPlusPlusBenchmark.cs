using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static System.Linq.Enumerable;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusBenchmark(DockerManager dockerManager) : BenchmarkBase<CPlusPlusBootstrap>(new CPlusPlusBootstrap(HarnessType.Benchmark), dockerManager)
{
    protected override string Render(ITestData data) =>
        $$"""
          #include <algorithm>
          #include <array>
          #include <chrono>
          #include <cstdint>
          #include <iomanip>
          #include <iostream>
          #include <limits>
          #include <locale>
          #include <string>

          template <class T>
          inline void DoNotOptimize(T& value) {
            asm volatile("" : "+g"(value) : : "memory");
          }

          {{data.Generate(Bootstrap.Generator)}}

          {{Bootstrap.Wrap($$"""
                                    auto keys = std::array{ {{FormatList(data.GetQuerySet(Bootstrap.Map), s => s)}} };
                                    std::array<double, {{data.SampleCount}}> results{};

                                    auto measure_sample = [&]() -> double
                                    {
                                        std::uint64_t found_count = 0;
                                        std::size_t key_index = 0;
                                        auto start = std::chrono::steady_clock::now();

                                        for (int i = 0; i < {{data.WorkIterations}}; i++)
                                        {
                                    {{FormatList(Range(0, data.QueryCount).ToArray(), _ =>
                                        """
                                                {
                                                    auto key = keys[key_index];
                                                    if (++key_index == keys.size())
                                                        key_index = 0;

                                                    DoNotOptimize(key);
                                                    found_count += fastdata::contains(key) ? 1 : 0;
                                                }
                                        """, "\n")}}
                                        }

                                        auto end = std::chrono::steady_clock::now();
                                        double elapsed_ns = std::chrono::duration<double, std::nano>(end - start).count();
                                        double elapsed_ns_per_call = elapsed_ns / {{Bootstrap.Map.ToValueLabel(data.WorkIterations * data.QueryCount)}};

                                        DoNotOptimize(found_count);
                                        std::uint64_t expected_found_count = {{Bootstrap.Map.ToValueLabel(data.WorkIterations * data.QueryCount)}};
                                        if (found_count != expected_found_count)
                                        {
                                            std::cerr << "Expected " << expected_found_count << " matches, got " << found_count << ".\n";
                                            std::exit(2);
                                        }

                                        return elapsed_ns_per_call;
                                    };

                                    for (int i = 0; i < {{data.WarmupCount}}; i++)
                                    {
                                        double elapsed = measure_sample();
                                        DoNotOptimize(elapsed);
                                    }

                                    double sum = 0.0;
                                    for (double& result : results)
                                    {
                                        result = measure_sample();
                                        sum += result;
                                    }

                                    std::sort(results.begin(), results.end());
                                    double avg = sum / results.size();

                                    std::cout.imbue(std::locale::classic());
                                    std::cout << std::setprecision(std::numeric_limits<double>::max_digits10)
                                              << results[0] << ' '
                                              << results[results.size() / 2] << ' '
                                              << results[results.size() - 1] << ' '
                                              << avg << '\n';

                                    return 0;
                             """)}}
          """;
}