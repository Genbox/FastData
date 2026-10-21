using Genbox.FastData.Generator.Ir.CSharp.Internal;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;

namespace Genbox.FastData.Generator.Ir.CSharp;

public sealed class CSharpIrCodeGenerator(CSharpIrCodeGeneratorConfig config) : IrCodeGenerator(config)
{
    public override IrBuiltinOperation SupportedBuiltins => IrBuiltins.All;

    protected override IrSourceEmitter CreateEmitter(PreparedLookup lookup) => new CSharpEmitter((CSharpIrCodeGeneratorConfig)Config, new EmissionContext(lookup));
}