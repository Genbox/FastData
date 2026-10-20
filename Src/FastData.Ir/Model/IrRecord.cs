namespace Genbox.FastData.Ir.Model;

/// <summary>A nominal record. Its identity is its position in <see cref="IrProgram.Records" />; a field's identity is its position in <see cref="Fields" />.</summary>
/// <remarks>Takes ownership of the field collection without copying it. Callers must not modify it through any alias; immutable fields may be shared.</remarks>
public sealed class IrRecord(string name, IReadOnlyList<IrField> fields)
{
    public string Name { get; } = name ?? throw Fail("A declaration name is required.");
    public IReadOnlyList<IrField> Fields { get; } = fields;
}