using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

public sealed class IrConversionExpression : IrExpression
{
    public IrConversionExpression(IrType targetType, ConversionKind conversion, IrExpression operand)
    {
        Type = targetType;
        Conversion = conversion;
        Operand = operand;
        IrType source = operand.Type;
        Require(Enum.IsDefined(conversion), "Unsupported conversion.");

        if (source.CharacterKind is {} sourceKind)
        {
            Require(conversion == ConversionKind.Numeric && targetType == IrType.UnitOf(sourceKind), "Characters convert only to their unit type: U8 for Ascii, U16 for Utf16CodeUnit.", code: IrDiagnosticCode.TypeMismatch);
            return;
        }

        if (targetType.CharacterKind is {} targetKind)
        {
            Require(conversion == ConversionKind.Numeric && source == IrType.UnitOf(targetKind), "Characters convert only from their unit type: U8 for Ascii, U16 for Utf16CodeUnit.", code: IrDiagnosticCode.TypeMismatch);
            return;
        }

        Require(source.IsNumeric && targetType.IsNumeric, "Conversions require numeric types.", code: IrDiagnosticCode.TypeMismatch);

        if (conversion == ConversionKind.BitCast)
        {
            // Integer-to-integer reinterpretation uses Numeric conversion; BitCast pairs floating-point and integer types.
            Require(source.IsFloatingPoint != targetType.IsFloatingPoint && source.BitWidth == targetType.BitWidth, "Bit casts require a floating-point type and an integral type of equal width.", code: IrDiagnosticCode.TypeMismatch);
            return;
        }

        Require(source.IsIntegral && targetType.IsIntegral, "Numeric conversions require integral types.", code: IrDiagnosticCode.TypeMismatch);
    }

    public ConversionKind Conversion { get; }
    public IrExpression Operand { get; }

    public IrType Type { get; }
}