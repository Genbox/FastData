using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Model;

/// <summary>An eager binary operation. Both operands evaluate exactly once in an unspecified order.</summary>
/// <remarks>Integral Divide and Modulo require a nonzero divisor on every executed path.</remarks>
public sealed class IrBinaryExpression(BinaryOperation operation, IrExpression left, IrExpression right) : IrExpression
{
    public BinaryOperation Operation { get; } = operation;
    public IrExpression Left { get; } = left;
    public IrExpression Right { get; } = right;
}