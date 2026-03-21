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
    protected override string Render(ITestData data) =>
        $"""
         {data.Generate(Bootstrap.Generator)}

         {Bootstrap.Wrap($$"""
                                   let mut found_count: u64 = 0;
                                   let keys = [ {{FormatList(Range(0, data.QueryCount)
                                                             .Select(_ => data.GetRandomKey(Bootstrap.Map))
                                                             .ToArray(), s => s)}} ];
                                   let mut key_index: usize = 0;

                                   let start = std::time::Instant::now();

                                   for _ in 0..{{data.WorkIterations}} {
                           {{FormatList(Range(0, data.QueryCount).ToArray(), _ =>
                               """
                                       {
                                           let key = keys[key_index];
                                           key_index += 1;
                                           if key_index == keys.len() {
                                               key_index = 0;
                                           }

                                           let key = std::hint::black_box(key);
                                           found_count += if fastdata::contains(key) { 1 } else { 0 };
                                       }
                               """, "\n")}}
                                   }

                                   let elapsed_ns = start.elapsed().as_secs_f64() * 1_000_000_000.0;
                                   let elapsed_ns_per_call = elapsed_ns / {{Bootstrap.Map.ToValueLabel(data.WorkIterations * data.QueryCount)}} as f64;

                                   std::hint::black_box(found_count);

                                   println!("{}", elapsed_ns_per_call);
                           """)}
         """;
}