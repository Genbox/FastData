using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

/// <summary>A loop that tests its condition before each execution of its body.</summary>
/// <remarks>The condition is required; use true for an infinite loop. Put iteration updates at the end of the body; break and return skip them.</remarks>
public sealed class IrLoopStatement : IrStatement
{
    public IrLoopStatement(IrExpression condition, IrBlock body)
    {
        Require(condition.Type == IrType.Bool, "Loop conditions must be Boolean.", code: IrDiagnosticCode.TypeMismatch);
        Condition = condition;
        Body = body;
    }

    public IrExpression Condition { get; }
    public IrBlock Body { get; }
}