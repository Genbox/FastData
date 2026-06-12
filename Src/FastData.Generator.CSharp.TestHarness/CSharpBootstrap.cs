using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpBootstrap : BootstrapBase
{
    // Benchmarks pin exact image digests so compiler/runtime updates do not silently change results.
    public CSharpBootstrap(HarnessType type) : base("CSharp", ".cs", type, CreateMap(), "mcr.microsoft.com/dotnet/sdk:10@sha256:548d93f8a18a1acbe6cc127bc4f47281430d34a9e35c18afa80a8d6741c2adc3", GetCommandTemplate(type)) { }

    public override ICodeGenerator Generator => new CSharpCodeGenerator(new CSharpCodeGeneratorConfig("FastData"));

    public override string Wrap(string code) =>
        $$"""
          public static class Program
          {
              public static int Main()
              {
          {{code}}
              }
          }
          """;

    public ExpressionCompiler CreateExpressionCompiler() => new CSharpExpressionCompiler(Map);

    private static TypeMap CreateMap()
    {
        CSharpLanguageDef langDef = new CSharpLanguageDef();
        return new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.Utf16CodeUnits);
    }

    private static string GetCommandTemplate(HarnessType type) =>
        type == HarnessType.Test
            ? "dotnet run -c Debug --property PublishAot=false -p:DebugType=None -p:DebugSymbols=false {0}"
            : "DOTNET_gcServer=0 DOTNET_gcConcurrent=1 DOTNET_ReadyToRun=0 DOTNET_TieredCompilation=0 DOTNET_TieredPGO=0 dotnet run -c Release --property PublishAot=false {0}";
}