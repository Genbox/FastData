namespace Genbox.FastData.Ir.Model;

public sealed class IrField(string name, IrType type)
{
    public string Name { get; } = name ?? throw Fail("A declaration name is required.");
    public IrType Type { get; } = type ?? throw Fail("A type is required.");
}