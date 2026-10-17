using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrU16Literal(ushort Value) : IrLiteral
{
    public override IrType Type => IrType.U16;
}