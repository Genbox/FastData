using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>Named immutable program data. Its identity is its position in <see cref="IrProgram.Data" />.</summary>
public sealed class IrData(string name, IrLiteral value)
{
    public string Name { get; } = name;
    public IrLiteral Value { get; } = value;

    public IrType Type => Value.Type;
}