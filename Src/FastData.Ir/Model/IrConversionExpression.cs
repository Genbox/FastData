using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Model;

public sealed class IrConversionExpression(IrType targetType, ConversionKind conversion, IrExpression operand) : IrExpression
{
    public IrType TargetType { get; } = targetType;
    public ConversionKind Conversion { get; } = conversion;
    public IrExpression Operand { get; } = operand;
}