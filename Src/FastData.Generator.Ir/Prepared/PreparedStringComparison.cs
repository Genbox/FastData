using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A Boolean string comparison using the program's code-unit equality and ordering.</summary>
public sealed class PreparedStringComparison(BinaryOperation operation, IrExpression left, IrExpression right) : IrExpression
{
    public IrType Type { get; } = IrType.Bool;
    public BinaryOperation Operation { get; } = operation;
    public IrExpression Left { get; } = left;
    public IrExpression Right { get; } = right;
}