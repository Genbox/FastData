using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrI64Literal(long Value) : IrLiteral
{
    public override IrType Type => IrType.I64;
}