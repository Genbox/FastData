using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.TestHarness.Code;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusBootstrap : BootstrapBase
{
    public CPlusPlusBootstrap(HarnessType type) : base("CPlusPlus", type)
    {
        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        Map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);
        Compiler = new GccCompiler(RootDir);
    }

    internal TypeMap Map { get; }
    internal GccCompiler Compiler { get; }

    public override ICodeGenerator GeneratorFactory(string id) => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id));
}