using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>An immutable named data value.</summary>
public sealed class PreparedData(string name, IrLiteral value, bool exported = false)
{
    public bool Exported { get; } = exported;
    public string Name { get; } = name;
    public IrLiteral Value { get; } = value;
    public IrType Type => Value.Type;
}