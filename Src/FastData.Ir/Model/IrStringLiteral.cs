using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed record IrStringLiteral(string Value) : IrLiteral
{
    public string Value { get; } = Value;
    public override IrType Type => IrType.String;
}