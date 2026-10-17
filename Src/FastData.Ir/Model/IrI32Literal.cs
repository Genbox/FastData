using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrI32Literal(int Value) : IrLiteral
{
    public override IrType Type => IrType.I32;
}