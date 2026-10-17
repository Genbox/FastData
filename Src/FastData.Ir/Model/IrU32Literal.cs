using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrU32Literal(uint Value) : IrLiteral
{
    public override IrType Type => IrType.U32;
}