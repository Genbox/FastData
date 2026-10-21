using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Generator.Ir.Rust.Internal;

namespace Genbox.FastData.Generator.Ir.Rust;

public sealed class RustIrCodeGenerator(IrCodeGeneratorConfig config) : IrCodeGenerator(config)
{
    public override IrBuiltinOperation SupportedBuiltins => RustText.SupportedBuiltins(Encoding);

    protected override IrSourceEmitter CreateEmitter(PreparedLookup lookup) => new RustEmitter(Config, new EmissionContext(lookup));
}