using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generator.Rust.TestHarness.Code;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustBootstrap : BootstrapBase
{
    public RustBootstrap(HarnessType type) : base("Rust", type)
    {
        RustLanguageDef langDef = new RustLanguageDef();
        Map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);
        Compiler = new RustCompiler(RootDir);
    }

    internal TypeMap Map { get; }
    internal RustCompiler Compiler { get; }

    public override ICodeGenerator GeneratorFactory(string id) => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id));
}