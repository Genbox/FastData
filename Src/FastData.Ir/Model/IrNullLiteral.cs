using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrNullLiteral(IrType Type) : IrLiteral
{
    public override IrType Type { get; } = Type;
}