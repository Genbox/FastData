namespace Genbox.FastData.Ir.Model;

/// <summary>A function parameter or local. Its identity is its position in the owning function.</summary>
public sealed class IrVariable(string name, IrType type)
{
    public string Name { get; } = name;
    public IrType Type { get; } = type;
}