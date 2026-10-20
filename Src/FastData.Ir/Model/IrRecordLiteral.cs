using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>An immutable record value with fields in declaration order. Field values are available through the Fields collection.</summary>
public sealed record IrRecordLiteral : IrLiteral
{
    /// <summary>Takes ownership of the field collection without copying it.</summary>
    /// <remarks>Callers must not modify the collection through any alias after construction; immutable literals may be shared.</remarks>
    public IrRecordLiteral(IrRecordId recordId, IReadOnlyList<IrLiteral> fields)
    {
        Type = IrType.Record(recordId);
        Fields = fields;
    }

    public IrRecordId RecordId => Type.RecordId!.Value;
    public IReadOnlyList<IrLiteral> Fields { get; }
    public override IrType Type { get; }
}