using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Rust.Internal;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustBootstrap : BootstrapBase
{
    // Benchmarks pin exact image digests so compiler/runtime updates do not silently change results.
    public RustBootstrap(HarnessType type) : base("Rust", ".rs", type, CreateMap(), "rust:1.96.0-bookworm@sha256:19817ead3289c8c631c73df281e18b59b172f6a31f4f563290f69cddd06c30e9", GetCommandTemplate(type), GetBuildCommandTemplate(type), GetRunCommandTemplate(type)) {}

    public override ICodeGenerator Generator => new RustCodeGenerator(new RustCodeGeneratorConfig("fastdata"));

    public override string Wrap(string code) =>
        $$"""
          fn main() {
          {{code}}
          }
          """;

    public ExpressionCompiler CreateExpressionCompiler() => new RustExpressionCompiler(Map);

    private static TypeMap CreateMap()
    {
        RustLanguageDef langDef = new RustLanguageDef();
        return new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.Utf8Bytes);
    }

    private static string GetCommandTemplate(HarnessType type) => type == HarnessType.Test
        ? "/bin/sh -c \"rustc -C debuginfo=0 -o {1} {0} && ./{1}\""
        : "/bin/sh -c \"rustc -O -C opt-level=3 -C lto -C codegen-units=1 -C debuginfo=0 -o {1} {0} && ./{1}\"";

    private static string? GetBuildCommandTemplate(HarnessType type) => type == HarnessType.Benchmark
        ? "/bin/sh -c \"rustc -O -C opt-level=3 -C lto -C codegen-units=1 -C debuginfo=0 -o {1} {0}\""
        : null;

    private static string? GetRunCommandTemplate(HarnessType type) => type == HarnessType.Benchmark ? "./{1} {2}" : null;
}