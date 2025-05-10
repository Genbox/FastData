using System.Globalization;
using System.Text;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.Generator.Rust.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.Rust.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "FastData", "Rust");
        Directory.CreateDirectory(rootPath);

        string benchPath = Path.Combine(rootPath, "benches");
        Directory.CreateDirectory(benchPath);

        // Build the Cargo.toml file
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("""
                      [package]
                      name = "fast_data_benchmarks"
                      version = "0.1.0"
                      edition = "2024"

                      [dev-dependencies]
                      criterion = "0.5.1"

                      """);

        foreach (ITestData data in TestVectorHelper.GetBenchmarkVectors())
        {
            data.Generate(id => new RustCodeGenerator(new RustCodeGeneratorConfig(id)), out GeneratorSpec spec);

            TestHelper.TryWriteFile(Path.Combine(benchPath, spec.Identifier + ".rs"),
                $$"""
                  #![allow(non_camel_case_types)]

                  {{spec.Source}}

                  use criterion::{criterion_group, criterion_main, Criterion};

                  fn bench_contains(c: &mut Criterion) {
                      c.bench_function("Rust_{{spec.Identifier}}", |b| {
                          b.iter(|| {
                  {{PrintQueries(data, spec.Identifier)}}
                          });
                      });
                  }

                  criterion_group!(benches, bench_contains);
                  criterion_main!(benches);
                  """);

            sb.AppendLine(CultureInfo.InvariantCulture, $"""
                                                         [[bench]]
                                                         name = "{spec.Identifier}"
                                                         harness = false
                                                         """);
        }

        TestHelper.TryWriteFile(Path.Combine(rootPath, "Cargo.toml"), sb.ToString());

        BenchmarkHelper.RunBenchmark("cargo", "bench", rootPath, "--adapter rust_criterion --testbed Rust");
    }

    private static string PrintQueries(ITestData data, string identifier)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
            sb.AppendLine(CultureInfo.InvariantCulture, $"           {identifier}::contains({data.GetValueLabel(ToValueLabel)});");

        return sb.ToString();
    }
}