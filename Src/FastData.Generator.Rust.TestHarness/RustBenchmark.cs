using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static System.Linq.Enumerable;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustBenchmark(DockerManager dockerManager) : BenchmarkBase<RustBootstrap>(new RustBootstrap(HarnessType.Benchmark), dockerManager)
{
    protected override string Render(ITestData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        BenchmarkQuerySet querySet = data.GetQuerySet(Bootstrap.Map);

        return $"""
                {data.Generate(Bootstrap.Generator)}

                {Bootstrap.Wrap($$"""
                                         let keys = [ {{FormatList(querySet.Keys, s => s)}} ];
                                         let mut results = [0.0f64; {{data.SampleCount}}];

                                         let mut measure_sample = || -> (f64, u64) {
                                             let mut found_count: u64 = 0;
                                             let mut key_index: usize = 0;
                                             let start = std::time::Instant::now();

                                             for _ in 0..{{data.WorkIterations}} {
                                         {{FormatList(Range(0, data.QueryCount).ToArray(), _ =>
                                             """
                                                     {
                                                         found_count += if fastdata::contains(std::hint::black_box(keys[key_index])) { 1 } else { 0 };
                                                         key_index += 1;
                                                         if key_index == keys.len() {
                                                             key_index = 0;
                                                         }
                                                     }
                                             """, "\n")}}
                                             }

                                             let elapsed_ns_per_call = (start.elapsed().as_secs_f64() * 1_000_000_000.0) / {{((long)data.WorkIterations * data.QueryCount).ToString(System.Globalization.CultureInfo.InvariantCulture)}}.0f64;

                                             std::hint::black_box(found_count);

                                             (elapsed_ns_per_call, found_count)
                                         };

                                         for _ in 0..{{data.WarmupCount}} {
                                             let (elapsed, found_count) = measure_sample();
                                             std::hint::black_box(elapsed);
                                             std::hint::black_box(found_count);
                                         }

                                         let mut sum = 0.0f64;
                                         let mut total_found_count: u64 = 0;
                                         for result in results.iter_mut() {
                                             let (elapsed, found_count) = measure_sample();
                                             *result = elapsed;
                                             total_found_count += found_count;
                                             sum += *result;
                                         }

                                         results.sort_by(|a, b| a.partial_cmp(b).unwrap());
                                         println!("{} {} {} {} {}", results[0], results[results.len() / 2], results[results.len() - 1], sum / results.len() as f64, total_found_count);
                                  """)}
                """;
    }
}