using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrBoolLiteral(bool Value) : IrLiteral
{
    public override IrType Type => IrType.Bool;
}