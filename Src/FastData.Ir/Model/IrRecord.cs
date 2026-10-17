namespace Genbox.FastData.Ir.Model;

/// <summary>A nominal record. Its identity is its position in <see cref="IrProgram.Records" />; a field's identity is its position in <see cref="Fields" />.</summary>
public sealed class IrRecord(string name, IReadOnlyList<IrField> fields)
{
    public string Name { get; } = name;
    public IReadOnlyList<IrField> Fields { get; } = fields;
}