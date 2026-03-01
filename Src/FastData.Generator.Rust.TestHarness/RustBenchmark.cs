using System.Globalization;
using System.Text;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustBenchmark : BenchmarkBase<RustBootstrap>
{
    private RustBenchmark() : base(new RustBootstrap(HarnessType.Benchmark)) {}

    public static RustBenchmark Instance { get; } = new RustBenchmark();

    public override BenchmarkSuite CreateFiles(IEnumerable<ITestData> data)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("""
                      [package]
                      name = "fast_data_benchmarks"
                      version = "0.1.0"
                      edition = "2024"

                      [dev-dependencies]
                      criterion = "0.8.1"

                      """);

        List<BenchmarkFile> files = new List<BenchmarkFile>();

        foreach (ITestData item in data)
        {
            item.Generate(Bootstrap.GeneratorFactory, out GeneratorSpec spec);

            files.Add(new BenchmarkFile(Path.Combine("benches", spec.Identifier + ".rs"),
                $$"""
                  #![allow(non_camel_case_types)]

                  {{spec.Source}}

                  use criterion::{criterion_group, criterion_main, Criterion};

                  fn bench_contains(c: &mut Criterion) {
                      c.bench_function("Rust_{{spec.Identifier}}", |b| {
                          b.iter(|| {
                  {{RenderBenchmarkQueries(item, spec.Identifier)}}
                          });
                      });
                  }

                  criterion_group!(benches, bench_contains);
                  criterion_main!(benches);
                  """));

            sb.AppendLine(CultureInfo.InvariantCulture, $"""
                                                         [[bench]]
                                                         name = "{spec.Identifier}"
                                                         harness = false
                                                         """);
        }

        return new BenchmarkSuite("Cargo.toml", sb.ToString(), files);
    }

    public override void Run(BenchmarkSuite suite, bool useBencher, bool useShell)
    {
        BenchmarkHelper.RunBenchmark("cargo", "bench", Bootstrap.RootDir, "--adapter rust_criterion --testbed Rust", useBencher, useShell);
    }

    private string RenderBenchmarkQueries(ITestData data, string identifier)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
            sb.AppendLine(CultureInfo.InvariantCulture, $"           let _ = {identifier}::contains({data.GetValueLabel(Bootstrap.Map)});");

        return sb.ToString();
    }
}