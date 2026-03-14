using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpBootstrap : BootstrapBase
{
    public CSharpBootstrap(HarnessType type) : base("CSharp", ".cs", type, "mcr.microsoft.com/dotnet/sdk:latest", GetCommandTemplate(type))
    {
        CSharpLanguageDef langDef = new CSharpLanguageDef();
        Map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF16);
    }

    internal TypeMap Map { get; }

    public override ICodeGenerator Generator => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig("FastData"));

    private static string GetCommandTemplate(HarnessType type) =>
        type == HarnessType.Test
            ? "dotnet run -c Debug -p:DebugType=None -p:DebugSymbols=false {0}"
            : "dotnet run -c Release {0}";
}