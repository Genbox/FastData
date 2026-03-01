using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpBootstrap : BootstrapBase
{
    public CSharpBootstrap(HarnessType type) : base("CSharp", type)
    {
        CSharpLanguageDef langDef = new CSharpLanguageDef();
        Map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF16);
    }

    internal TypeMap Map { get; }

    public override ICodeGenerator GeneratorFactory(string id) => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id));
}