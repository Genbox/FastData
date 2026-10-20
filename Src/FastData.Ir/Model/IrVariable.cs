namespace Genbox.FastData.Ir.Model;

/// <summary>A function parameter or local. Its identity is its position in the owning function.</summary>
public sealed class IrVariable(string name, IrType type)
{
    public string Name { get; } = name ?? throw Fail("A declaration name is required.");
    public IrType Type { get; } = type ?? throw Fail("A type is required.");
}