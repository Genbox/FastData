using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>An array read. Collection and index evaluate exactly once in an unspecified order.</summary>
/// <remarks>The producer must ensure a non-null collection and an index within its bounds on every executed path.</remarks>
public sealed class IrIndexExpression(IrExpression collection, IrExpression index) : IrExpression
{
    public IrExpression Collection { get; } = collection;
    public IrExpression Index { get; } = index;
}