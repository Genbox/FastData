using Genbox.FastData.Generator.Ir.CPlusPlus.Internal;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;

namespace Genbox.FastData.Generator.Ir.CPlusPlus;

public sealed class CPlusPlusIrCodeGenerator(IrCodeGeneratorConfig config) : IrCodeGenerator(config)
{
    public override IrBuiltinOperation SupportedBuiltins => IrBuiltins.Required;

    protected override IrSourceEmitter CreateEmitter(PreparedLookup lookup) => new CPlusPlusEmitter(Config, new EmissionContext(lookup));
}