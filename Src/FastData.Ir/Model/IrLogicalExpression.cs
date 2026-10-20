using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

/// <summary>A Boolean operation that evaluates Left once and Right once only when required.</summary>
public sealed class IrLogicalExpression : IrExpression
{
    public IrLogicalExpression(LogicalOperation operation, IrExpression left, IrExpression right)
    {
        Operation = operation;
        Left = left;
        Right = right;
        Require(Enum.IsDefined(operation), "Unsupported logical operation.");
        Require(left.Type == IrType.Bool && right.Type == IrType.Bool, "Logical operands must be Boolean.", code: IrDiagnosticCode.TypeMismatch);
    }

    public LogicalOperation Operation { get; }
    public IrExpression Left { get; }
    public IrExpression Right { get; }

    public IrType Type => IrType.Bool;
}