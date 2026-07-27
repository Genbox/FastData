using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpBootstrap : BootstrapBase
{
    // Correctness tests compile hundreds of independent programs. Invoke Roslyn directly to avoid
    // running MSBuild and restore for every file-based app.
    private const string TestCommand = "rsp=/tmp/fastdata-csharp-refs.rsp; " +
                                       "if [ ! -f \"$rsp\" ]; then find /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0 -maxdepth 1 -name '*.dll' -printf '-r:%p\\n' > \"$rsp\"; fi; " +
                                       "dotnet /usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc.dll -shared -nologo -noconfig -nostdlib+ -debug- -optimize- -langversion:latest -target:exe -out:/tmp/{1}.dll @\"$rsp\" {0} && " +
                                       "dotnet exec --runtimeconfig /usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc.runtimeconfig.json /tmp/{1}.dll";

    // Benchmarks pin exact image digests so compiler/runtime updates do not silently change results.
    public CSharpBootstrap(HarnessType type) : base("CSharp", ".cs", type, CreateMap(), "mcr.microsoft.com/dotnet/sdk:10@sha256:548d93f8a18a1acbe6cc127bc4f47281430d34a9e35c18afa80a8d6741c2adc3", GetCommandTemplate(type), GetBuildCommandTemplate(type), GetRunCommandTemplate(type)) {}

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
            ? TestCommand
            : "DOTNET_gcServer=0 DOTNET_gcConcurrent=1 DOTNET_ReadyToRun=0 DOTNET_TieredCompilation=0 DOTNET_TieredPGO=0 dotnet run -c Release --property PublishAot=false {0}";

    private static string? GetBuildCommandTemplate(HarnessType type) => type == HarnessType.Benchmark
        ? "dotnet publish -c Release --property PublishAot=false -p:DebugType=None -p:DebugSymbols=false -o {1}-out {0}"
        : null;

    private static string? GetRunCommandTemplate(HarnessType type) => type == HarnessType.Benchmark
        ? "DOTNET_gcServer=0 DOTNET_gcConcurrent=1 DOTNET_ReadyToRun=0 DOTNET_TieredCompilation=0 DOTNET_TieredPGO=0 /bin/sh -c \"if [ -f {1}-out/{1}.dll ]; then dotnet {1}-out/{1}.dll {2}; else {1}-out/{1} {2}; fi\""
        : null;
}