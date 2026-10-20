using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

/// <summary>An array read. Collection and index evaluate exactly once in an unspecified order.</summary>
/// <remarks>The producer must ensure a non-null collection and an index within its bounds on every executed path.</remarks>
public sealed class IrIndexExpression : IrExpression
{
    public IrIndexExpression(IrExpression collection, IrExpression index)
    {
        Require(collection.Type.Kind == IrTypeKind.Array, "Indexing requires an array.", code: IrDiagnosticCode.TypeMismatch);
        Require(index.Type == IrType.I32, "Array indices must be I32.", code: IrDiagnosticCode.TypeMismatch);
        Type = collection.Type.ElementType!;
        Collection = collection;
        Index = index;
    }

    public IrExpression Collection { get; }
    public IrExpression Index { get; }

    public IrType Type { get; }
}