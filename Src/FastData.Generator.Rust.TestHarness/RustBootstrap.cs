using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Rust.Internal;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustBootstrap : BootstrapBase
{
    public RustBootstrap(HarnessType type) : base("Rust", ".rs", type, "rust:slim-trixie", GetCommandTemplate(type))
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