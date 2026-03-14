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

          int main()
          {
              std::uint64_t found_count = 0;
              auto keys = std::array{ {{FormatList(Range(0, data.QueryCount)
                                                   .Select(_ => data.GetRandomKey(Bootstrap.Map))
                                                   .ToArray(), s => s)}} };
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

              std::cout.imbue(std::locale::classic());
              std::cout << std::setprecision(std::numeric_limits<double>::max_digits10)
                        << elapsed_ns_per_call << '\n';

              return 0;
          }
          """;
}