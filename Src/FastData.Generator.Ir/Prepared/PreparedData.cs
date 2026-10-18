using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>An immutable declaration with its literal and array layout.</summary>
public sealed class PreparedData(string name, IrLiteral value, bool exported = false)
{
    public bool Exported { get; } = exported;
    public string Name { get; } = name;
    public IrLiteral Value { get; } = value;
    public IrType Type => Value.Type;
    public IrArrayLiteralBase? Array { get; } = value as IrArrayLiteralBase;
    public bool Inline => Array == null || Array.Count <= 8;
    public int ElementsPerLine => Array?.ElementType.Kind == IrTypeKind.Record ? 1 : 8;
}