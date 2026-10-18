using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>Unsigned arithmetic whose result wraps at the operand width.</summary>
public sealed class PreparedWrappingArithmetic(IrType type, BinaryOperation operation, IrExpression left, IrExpression right) : IrExpression
{
    public IrType Type { get; } = type;
    public BinaryOperation Operation { get; } = operation;
    public IrExpression Left { get; } = left;
    public IrExpression Right { get; } = right;
}