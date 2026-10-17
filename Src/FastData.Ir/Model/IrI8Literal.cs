using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrI8Literal(sbyte Value) : IrLiteral
{
    public override IrType Type => IrType.I8;
}