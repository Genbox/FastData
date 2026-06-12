using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static System.Linq.Enumerable;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusBenchmark(DockerManager dockerManager) : BenchmarkBase<CPlusPlusBootstrap>(new CPlusPlusBootstrap(HarnessType.Benchmark), dockerManager)
{
    protected override string Render(ITestData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        BenchmarkQuerySet querySet = data.GetQuerySet(Bootstrap.Map);

        return $$"""
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
                                          auto keys = std::array{ {{FormatList(querySet.Keys, s => s)}} };
                                          std::array<double, {{data.SampleCount}}> results{};

                                          auto measure_sample = [&](std::uint64_t& found_count) -> double
                                          {
                                              found_count = 0;
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

                                              double elapsed_ns_per_call = std::chrono::duration<double, std::nano>(std::chrono::steady_clock::now() - start).count() / {{((long)data.WorkIterations * data.QueryCount).ToString(System.Globalization.CultureInfo.InvariantCulture)}}.0;

                                              DoNotOptimize(found_count);

                                              return elapsed_ns_per_call;
                                          };

                                          for (int i = 0; i < {{data.WarmupCount}}; i++)
                                          {
                                              std::uint64_t warmup_found_count = 0;
                                              double elapsed = measure_sample(warmup_found_count);
                                              DoNotOptimize(elapsed);
                                              DoNotOptimize(warmup_found_count);
                                          }

                                          double sum = 0.0;
                                          std::uint64_t total_found_count = 0;
                                          for (double& result : results)
                                          {
                                              std::uint64_t sample_found_count = 0;
                                              result = measure_sample(sample_found_count);
                                              total_found_count += sample_found_count;
                                              sum += result;
                                          }

                                          std::sort(results.begin(), results.end());

                                          std::cout.imbue(std::locale::classic());
                                          std::cout << std::setprecision(std::numeric_limits<double>::max_digits10)
                                                    << results[0] << ' '
                                                    << results[results.size() / 2] << ' '
                                                    << results[results.size() - 1] << ' '
                                                    << sum / results.size() << ' '
                                                    << total_found_count << '\n';

                                          return 0;
                                    """)}}
                 """;
    }
}