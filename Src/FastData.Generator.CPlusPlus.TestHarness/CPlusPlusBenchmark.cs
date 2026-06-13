using System.Globalization;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
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
                 #include <cstdlib>
                 #include <iomanip>
                 #include <iostream>
                 #include <limits>
                 #include <locale>
                 #include <string>
                 #include <string_view>

                 template <class T>
                 inline void DoNotOptimize(const T& value) {
                   asm volatile("" : : "r,m"(value) : "memory");
                 }

                 {{data.Generate(Bootstrap.Generator)}}

                 int main(int argc, char** argv)
                 {
                     if (argc != 4)
                         return 2;

                     std::uint64_t invocation_count = std::strtoull(argv[1], nullptr, 10);
                     int warmup_count = std::atoi(argv[2]);
                     int sample_count = std::atoi(argv[3]);
                     {{GetKeysDeclaration(data, querySet.Keys)}}

                     auto measure_baseline = [&](std::uint64_t invocations) -> double
                     {
                         std::size_t key_index = 0;
                         auto start = std::chrono::steady_clock::now();

                         for (std::uint64_t i = 0; i < invocations; i++)
                         {
                             const auto& key = keys[key_index];
                             if (++key_index == keys.size())
                                 key_index = 0;

                             DoNotOptimize(key);
                         }

                         return std::chrono::duration<double, std::nano>(std::chrono::steady_clock::now() - start).count();
                     };

                     auto measure_lookup = [&](std::uint64_t invocations, std::uint64_t& found_count) -> double
                     {
                         found_count = 0;
                         std::size_t key_index = 0;
                         auto start = std::chrono::steady_clock::now();

                         for (std::uint64_t i = 0; i < invocations; i++)
                         {
                             const auto& key = keys[key_index];
                             if (++key_index == keys.size())
                                 key_index = 0;

                             DoNotOptimize(key);
                             found_count += fastdata::contains(key) ? 1 : 0;
                         }

                         double elapsed_ns = std::chrono::duration<double, std::nano>(std::chrono::steady_clock::now() - start).count();

                         DoNotOptimize(found_count);

                         return elapsed_ns;
                     };

                     for (int i = 0; i < warmup_count; i++)
                     {
                         double elapsed = measure_baseline(invocation_count);
                         DoNotOptimize(elapsed);
                     }

                     std::cout.imbue(std::locale::classic());

                     for (int i = 0; i < warmup_count; i++)
                     {
                         std::uint64_t warmup_found_count = 0;
                         double elapsed = measure_lookup(invocation_count, warmup_found_count);
                         DoNotOptimize(elapsed);
                         DoNotOptimize(warmup_found_count);
                     }

                     for (int i = 0; i < sample_count; i++)
                     {
                         double overhead = measure_baseline(invocation_count);
                         std::cout << std::setprecision(std::numeric_limits<double>::max_digits10)
                                   << "overhead " << overhead << '\n';

                         std::uint64_t sample_found_count = 0;
                         double elapsed = measure_lookup(invocation_count, sample_found_count);
                         std::cout << std::setprecision(std::numeric_limits<double>::max_digits10)
                                   << "sample " << elapsed << ' '
                                   << sample_found_count << '\n';
                     }

                     return 0;
                 }
                 """;
    }

    private static string GetKeysDeclaration(ITestData data, string[] keys)
    {
        string values = FormatList(keys, static s => s);

        if (data.KeyType == typeof(string))
            return $"std::array<std::string_view, {keys.Length.ToString(CultureInfo.InvariantCulture)}> keys = {{ {values} }};";

        return $"auto keys = std::array{{ {values} }};";
    }
}