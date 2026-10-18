using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generator.Ir.Tests.Code;

internal sealed class PreparingGenerator(GeneratorEncoding encoding = GeneratorEncoding.Utf16CodeUnits, IrBuiltinOperation builtins = IrBuiltins.All) : ICodeGenerator
{
    public PreparedLookup Lookup { get; private set; } = null!;
    public GeneratorEncoding Encoding => encoding;

    public string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context)
    {
        Lookup = IrGeneratorPreparation.Prepare<TKey, TValue>(genCfg, context, new IrBackendCapabilities(encoding, builtins));
        return string.Empty;
    }
}