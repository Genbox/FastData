using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
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

        foreach ((StructureType type, object[] data) in TestVectorHelper.GetBenchmarkVectors())
        {
            if (!TestVectorHelper.TryGenerate(id => new RustCodeGenerator(new RustCodeGeneratorConfig(id)), type, data, out GeneratorSpec spec))
                throw new InvalidOperationException("Unable to build " + type);

            TestHelper.TryWriteFile(Path.Combine(benchPath, spec.Identifier + ".rs"),
                $$"""
                  #![allow(non_camel_case_types)]

                  {{spec.Source}}

                  use criterion::{criterion_group, criterion_main, Criterion};

                  fn bench_contains(c: &mut Criterion) {
                      c.bench_function("Rust_{{spec.Identifier}}", |b| {
                          b.iter(|| {
                              {{spec.Identifier}}::contains({{ToValueLabel(data[0])}})
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
}