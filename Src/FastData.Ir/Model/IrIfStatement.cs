using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed class IrIfStatement(IrExpression condition, IrBlock ifTrue, IrBlock? ifFalse = null) : IrStatement
{
    public IrExpression Condition { get; } = condition;
    public IrBlock IfTrue { get; } = ifTrue;
    public IrBlock? IfFalse { get; } = ifFalse;
}