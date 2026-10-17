using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrU64Literal(ulong Value) : IrLiteral
{
    public override IrType Type => IrType.U64;
}