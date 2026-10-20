using Genbox.FastData.Ir.Extensions;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

/// <summary>An eager binary operation. Both operands evaluate exactly once in an unspecified order.</summary>
/// <remarks>Integral Divide and Modulo require a nonzero divisor on every executed path.</remarks>
public sealed class IrBinaryExpression : IrExpression
{
    public IrBinaryExpression(BinaryOperation operation, IrExpression left, IrExpression right)
    {
        Operation = operation;
        Left = left;
        Right = right;
        Type = operation.IsComparison() ? IrType.Bool : left.Type;
        IrType leftType = left.Type;
        IrType rightType = right.Type;

        switch (operation)
        {
            case BinaryOperation.Add or BinaryOperation.Subtract or BinaryOperation.Multiply:
                Require(leftType == rightType && leftType.IsIntegral, "Arithmetic requires matching integral operands.", code: IrDiagnosticCode.TypeMismatch);
                return;
            case BinaryOperation.Divide or BinaryOperation.Modulo:
                Require(leftType == rightType && leftType.IsUnsignedIntegral, "Division and modulo require matching unsigned integral operands.", code: IrDiagnosticCode.TypeMismatch);
                return;
            case BinaryOperation.And or BinaryOperation.Or or BinaryOperation.ExclusiveOr:
                Require(leftType == rightType && leftType.IsIntegral, "Bitwise operators require matching integral operands.", code: IrDiagnosticCode.TypeMismatch);
                return;
            case BinaryOperation.ShiftLeft or BinaryOperation.ShiftRight:
                Require(leftType.IsUnsignedIntegral && rightType == IrType.I32, "Shifts require an unsigned integral value and an I32 count.", code: IrDiagnosticCode.TypeMismatch);
                return;
            case BinaryOperation.Equal or BinaryOperation.NotEqual:
                Require(leftType == rightType, "Equality requires matching operand types.", code: IrDiagnosticCode.TypeMismatch);
                Require(leftType.Kind is not (IrTypeKind.Array or IrTypeKind.Record) || left is IrNullLiteral || right is IrNullLiteral, "Arrays and records support only equality checks against a null literal; compare elements or fields explicitly.", code: IrDiagnosticCode.TypeMismatch);
                return;
            case BinaryOperation.LessThan or BinaryOperation.LessThanOrEqual or BinaryOperation.GreaterThan or BinaryOperation.GreaterThanOrEqual:
                Require(leftType == rightType && (leftType.IsNumeric || leftType.Kind == IrTypeKind.Char || leftType == IrType.String), "Ordered comparisons require matching numeric, character, or String operands.", code: IrDiagnosticCode.TypeMismatch);
                return;
            default:
                throw Fail("Unsupported binary operation.");
        }
    }

    public BinaryOperation Operation { get; }
    public IrExpression Left { get; }
    public IrExpression Right { get; }

    public IrType Type { get; }
}