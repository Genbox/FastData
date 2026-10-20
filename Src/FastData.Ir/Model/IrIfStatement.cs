using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

public sealed class IrIfStatement : IrStatement
{
    public IrIfStatement(IrExpression condition, IrBlock ifTrue, IrBlock? ifFalse = null)
    {
        Require(condition.Type == IrType.Bool, "Branch conditions must be Boolean.", code: IrDiagnosticCode.TypeMismatch);
        Condition = condition;
        IfTrue = ifTrue;
        IfFalse = ifFalse;
    }

    public IrExpression Condition { get; }
    public IrBlock IfTrue { get; }
    public IrBlock? IfFalse { get; }
}