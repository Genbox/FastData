using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Validation.IrValidation;

namespace Genbox.FastData.Ir.Validation;

/// <summary>Validates operand types and determines result types for IR operations.</summary>
internal static class IrOperationRules
{
    public static IrType Binary(IrBinaryExpression expression, IrType left, IrType right, IrDiagnosticPath path)
    {
        switch (expression.Operation)
        {
            case BinaryOperation.Add or BinaryOperation.Subtract or BinaryOperation.Multiply:
                Require(left == right && left.IsIntegral, "Arithmetic requires matching integral operands.", path, IrDiagnosticCode.TypeMismatch);
                return left;
            case BinaryOperation.Divide or BinaryOperation.Modulo:
                Require(left == right && left.IsUnsignedIntegral, "Division and modulo require matching unsigned integral operands.", path, IrDiagnosticCode.TypeMismatch);
                return left;
            case BinaryOperation.And or BinaryOperation.Or or BinaryOperation.ExclusiveOr:
                Require(left == right && left.IsIntegral, "Bitwise operators require matching integral operands.", path, IrDiagnosticCode.TypeMismatch);
                return left;
            case BinaryOperation.ShiftLeft or BinaryOperation.ShiftRight:
                Require(left.IsUnsignedIntegral && right == IrType.I32, "Shifts require an unsigned integral value and an I32 count.", path, IrDiagnosticCode.TypeMismatch);
                return left;
            case BinaryOperation.Equal or BinaryOperation.NotEqual:
                Require(left == right, "Equality requires matching operand types.", path, IrDiagnosticCode.TypeMismatch);
                Require(left.Kind is not (IrTypeKind.Array or IrTypeKind.Record) || expression.Left is IrNullLiteral || expression.Right is IrNullLiteral, "Arrays and records support only equality checks against a null literal; compare elements or fields explicitly.", path, IrDiagnosticCode.TypeMismatch);
                return IrType.Bool;
            case BinaryOperation.LessThan or BinaryOperation.LessThanOrEqual or BinaryOperation.GreaterThan or BinaryOperation.GreaterThanOrEqual:
                Require(left == right && (left.IsNumeric || left.Kind == IrTypeKind.Char || left == IrType.String), "Ordered comparisons require matching numeric, character, or String operands.", path, IrDiagnosticCode.TypeMismatch);
                return IrType.Bool;
            default:
                throw Fail("Unsupported binary operation.", path + ".Operation");
        }
    }

    public static IrType Conversion(ConversionKind kind, IrType source, IrType target, IrDiagnosticPath path)
    {
        Require(Enum.IsDefined(kind), "Unsupported conversion.", path + ".Conversion");

        if (source.CharacterKind is {} sourceKind)
        {
            Require(kind == ConversionKind.Numeric && target == IrType.UnitOf(sourceKind), "Characters convert only to their unit type: U8 for Ascii, U16 for Utf16CodeUnit.", path, IrDiagnosticCode.TypeMismatch);
            return target;
        }

        if (target.CharacterKind is {} targetKind)
        {
            Require(kind == ConversionKind.Numeric && source == IrType.UnitOf(targetKind), "Characters convert only from their unit type: U8 for Ascii, U16 for Utf16CodeUnit.", path, IrDiagnosticCode.TypeMismatch);
            return target;
        }

        Require(source.IsNumeric && target.IsNumeric, "Conversions require numeric types.", path, IrDiagnosticCode.TypeMismatch);

        if (kind == ConversionKind.BitCast)
        {
            // Integer-to-integer reinterpretation uses Numeric conversion; BitCast pairs floating-point and integer types.
            Require(source.IsFloatingPoint != target.IsFloatingPoint && source.BitWidth == target.BitWidth, "Bit casts require a floating-point type and an integral type of equal width.", path, IrDiagnosticCode.TypeMismatch);
            return target;
        }

        Require(source.IsIntegral && target.IsIntegral, "Numeric conversions require integral types.", path, IrDiagnosticCode.TypeMismatch);

        return target;
    }

    public static IrType Logical(LogicalOperation operation, IrType left, IrType right, IrDiagnosticPath path)
    {
        Require(Enum.IsDefined(operation), "Unsupported logical operation.", path + ".Operation");
        Require(left == IrType.Bool && right == IrType.Bool, "Logical operands must be Boolean.", path, IrDiagnosticCode.TypeMismatch);
        return IrType.Bool;
    }
}