using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>A loop that tests its condition before each execution of its body.</summary>
/// <remarks>The condition is required; use true for an infinite loop. Put iteration updates at the end of the body; break and return skip them.</remarks>
public sealed class IrLoopStatement(IrExpression condition, IrBlock body) : IrStatement
{
    public IrExpression Condition { get; } = condition;
    public IrBlock Body { get; } = body;
}