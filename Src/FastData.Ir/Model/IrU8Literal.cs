using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrU8Literal(byte Value) : IrLiteral
{
    public override IrType Type => IrType.U8;
}