namespace Genbox.FastData.Ir.Model;

public sealed class IrField(string name, IrType type)
{
    public string Name { get; } = name;
    public IrType Type { get; } = type;
}