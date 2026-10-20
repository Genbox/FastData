using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A bit reinterpretation with source and target types resolved for the runtime adapter.</summary>
public sealed class PreparedBitCast(IrType sourceType, IrType targetType, IrExpression operand) : IrExpression
{
    public IrType Type { get; } = targetType;
    public IrType SourceType { get; } = sourceType;
    public IrType TargetType { get; } = targetType;
    public IrExpression Operand { get; } = operand;
}