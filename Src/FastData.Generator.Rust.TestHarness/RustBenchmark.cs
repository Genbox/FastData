using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
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
                                          let args: Vec<String> = std::env::args().collect();
                                          if args.len() != 4 {
                                              std::process::exit(2);
                                          }

                                          let invocation_count: u64 = args[1].parse().unwrap();
                                          let warmup_count: usize = args[2].parse().unwrap();
                                          let sample_count: usize = args[3].parse().unwrap();

                                          let mut measure_baseline = |invocations: u64| -> f64 {
                                              let mut key_index: usize = 0;
                                              let start = std::time::Instant::now();

                                              for _ in 0..invocations {
                                                  std::hint::black_box(keys[key_index]);
                                                  key_index += 1;
                                                  if key_index == keys.len() {
                                                      key_index = 0;
                                                  }
                                              }

                                              start.elapsed().as_secs_f64() * 1_000_000_000.0
                                          };

                                          let mut measure_lookup = |invocations: u64| -> (f64, u64) {
                                              let mut found_count: u64 = 0;
                                              let mut key_index: usize = 0;
                                              let start = std::time::Instant::now();

                                              for _ in 0..invocations {
                                                  found_count += if fastdata::contains(std::hint::black_box(keys[key_index])) { 1 } else { 0 };
                                                  key_index += 1;
                                                  if key_index == keys.len() {
                                                      key_index = 0;
                                                  }
                                              }

                                              let elapsed_ns = start.elapsed().as_secs_f64() * 1_000_000_000.0;

                                              std::hint::black_box(found_count);

                                              (elapsed_ns, found_count)
                                          };

                                          for _ in 0..warmup_count {
                                              let elapsed = measure_baseline(invocation_count);
                                              std::hint::black_box(elapsed);
                                          }

                                          for _ in 0..warmup_count {
                                              let (elapsed, found_count) = measure_lookup(invocation_count);
                                              std::hint::black_box(elapsed);
                                              std::hint::black_box(found_count);
                                          }

                                          for _ in 0..sample_count {
                                              let overhead = measure_baseline(invocation_count);
                                              println!("overhead {}", overhead);

                                              let (elapsed, found_count) = measure_lookup(invocation_count);
                                              println!("sample {} {}", elapsed, found_count);
                                          }
                                  """)}
                """;
    }
}