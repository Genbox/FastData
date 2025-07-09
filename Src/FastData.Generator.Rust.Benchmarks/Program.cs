using System.Globalization;
using System.Text;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Generator.Rust.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "Rust");
        TestHelper.CreateOrEmptyDirectory(rootDir);

        string benchPath = Path.Combine(rootDir, "benches");
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

        foreach (ITestData data in TestVectorHelper.GetBenchmarkData())
        {
            data.Generate(id => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id)), out GeneratorSpec spec);

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

        TestHelper.TryWriteFile(Path.Combine(rootDir, "Cargo.toml"), sb.ToString());

        BenchmarkHelper.RunBenchmark("cargo", "bench", rootDir, "--adapter rust_criterion --testbed Rust");
    }

    private static string PrintQueries(ITestData data, string identifier)
    {
        RustLanguageDef langDef = new RustLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, langDef.Encoding);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"           let _ = {identifier}::contains({data.GetValueLabel(map)});");
        }

        return sb.ToString();
    }
}