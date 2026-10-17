using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Model;

/// <summary>A Boolean operation that evaluates Left once and Right once only when required.</summary>
public sealed class IrLogicalExpression(LogicalOperation operation, IrExpression left, IrExpression right) : IrExpression
{
    public LogicalOperation Operation { get; } = operation;
    public IrExpression Left { get; } = left;
    public IrExpression Right { get; } = right;
}