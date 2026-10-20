using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

/// <summary>Reads a field of a record value. The receiver is evaluated once.</summary>
/// <remarks>The producer must ensure a non-null record receiver on every executed path.</remarks>
public sealed class IrFieldExpression : IrExpression
{
    public IrFieldExpression(IrExpression receiver, IrFieldId fieldId)
    {
        Require(receiver.Type.Kind == IrTypeKind.Record && receiver.Type.RecordId == fieldId.RecordId, "Field reads require a receiver of the field's declaring record type.", code: IrDiagnosticCode.TypeMismatch);
        Receiver = receiver;
        FieldId = fieldId;
    }

    public IrExpression Receiver { get; }
    public IrFieldId FieldId { get; }

    public IrType Type => FieldId.Type;
}