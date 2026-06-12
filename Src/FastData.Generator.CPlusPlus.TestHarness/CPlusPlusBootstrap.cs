using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusBootstrap : BootstrapBase
{
    // Benchmarks pin exact image digests so compiler/runtime updates do not silently change results.
    public CPlusPlusBootstrap(HarnessType type) : base("CPlusPlus", ".cpp", type, "silkeh/clang:21-bookworm@sha256:c735c54928e1b8be46101c9aff2895f72d1bfb6d94f713804528fa2fd0a96dba", GetCommandTemplate(type))
    {
        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        Map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.Utf8Bytes);
    }

    public TypeMap Map { get; }

    public override ICodeGenerator Generator => new CPlusPlusCodeGenerator(new CPlusPlusCodeGeneratorConfig("fastdata"));

    public override string Wrap(string code) =>
        $$"""
          int main()
          {
          {{code}}
          }
          """;

    public ExpressionCompiler CreateExpressionCompiler() => new CPlusPlusExpressionCompiler(Map);

    private static string GetCommandTemplate(HarnessType type) => type == HarnessType.Test
        ? "/bin/sh -c \"clang++ -O0 -g0 -std=c++17 -o {1} {0} && ./{1}\""
        : "/bin/sh -c \"clang++ -O3 -DNDEBUG -std=c++17 -o {1} {0} && ./{1}\"";
}