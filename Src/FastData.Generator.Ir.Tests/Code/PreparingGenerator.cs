using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generator.Ir.Tests.Code;

internal sealed class PreparingGenerator(GeneratorEncoding encoding = GeneratorEncoding.Utf16CodeUnits, IrBuiltinOperation builtins = IrBuiltins.All) : IrCodeGenerator(new IrCodeGeneratorConfig("LookupTable", encoding)), ICodeGenerator
{
    public PreparedLookup Lookup { get; private set; } = null!;
    public override IrBuiltinOperation SupportedBuiltins => builtins;

    string ICodeGenerator.Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context)
    {
        Lookup = Prepare<TKey, TValue>(genCfg, context);
        return string.Empty;
    }

    public new PreparedLookup Prepare(FastDataProgram lookup) => base.Prepare(lookup);

    protected override IrSourceEmitter CreateEmitter(PreparedLookup lookup) => throw new NotSupportedException("This test generator only prepares lookups.");
}