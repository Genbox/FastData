using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Rust.Internal;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustBootstrap : BootstrapBase
{
    // Benchmarks pin exact image digests so compiler/runtime updates do not silently change results.
    public RustBootstrap(HarnessType type) : base("Rust", ".rs", type, "rust:1.96.0-bookworm@sha256:19817ead3289c8c631c73df281e18b59b172f6a31f4f563290f69cddd06c30e9", GetCommandTemplate(type))
    {
        RustLanguageDef langDef = new RustLanguageDef();
        Map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.Utf8Bytes);
    }

    public TypeMap Map { get; }

    public override ICodeGenerator Generator => new RustCodeGenerator(new RustCodeGeneratorConfig("fastdata"));

    public override string Wrap(string code) =>
        $$"""
          fn main() {
          {{code}}
          }
          """;

    public ExpressionCompiler CreateExpressionCompiler() => new RustExpressionCompiler(Map);

    private static string GetCommandTemplate(HarnessType type) => type == HarnessType.Test
        ? "/bin/sh -c \"rustc -C debuginfo=0 -o {1} {0} && ./{1}\""
        : "/bin/sh -c \"rustc -O -C opt-level=3 -C lto -C codegen-units=1 -C debuginfo=0 -o {1} {0} && ./{1}\"";
}