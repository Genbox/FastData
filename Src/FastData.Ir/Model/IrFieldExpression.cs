using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>Reads a field of a record value. The receiver is evaluated once.</summary>
/// <remarks>The producer must ensure a non-null record receiver on every executed path.</remarks>
public sealed class IrFieldExpression(IrExpression receiver, IrFieldId fieldId) : IrExpression
{
    public IrExpression Receiver { get; } = receiver;
    public IrFieldId FieldId { get; } = fieldId;
}