using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Traverses prepared statement bodies using backend statement and block syntax.</summary>
public abstract class StatementWriter
{
    protected void Statements(IrBlock block)
    {
        foreach (IrStatement statement in block.Statements)
        {
            switch (statement)
            {
                case IrAssignmentStatement assignment:
                    WriteAssignment(assignment);
                    break;
                case IrReturnStatement result:
                    WriteReturn(result);
                    break;
                case IrBreakStatement:
                    WriteBreak();
                    break;
                case IrIfStatement conditional:
                    WriteIf(conditional.Condition);
                    Body(conditional.IfTrue);

                    if (conditional.IfFalse is {} otherwise)
                    {
                        WriteElse();
                        Body(otherwise);
                    }

                    break;
                case IrLoopStatement loop:
                    WriteLoop(loop.Condition);
                    Body(loop.Body);
                    break;
                case PreparedSwitch dispatch:
                    WriteSwitch(dispatch);
                    break;
                default:
                    throw new NotSupportedException($"Statement '{statement.GetType().Name}' is not supported by emission.");
            }
        }
    }

    private void Body(IrBlock block)
    {
        OpenBlock();
        Statements(block);
        CloseBlock();
    }

    protected abstract void WriteAssignment(IrAssignmentStatement assignment);
    protected abstract void WriteReturn(IrReturnStatement result);
    protected abstract void WriteBreak();
    protected abstract void WriteIf(IrExpression condition);
    protected abstract void WriteElse();
    protected abstract void WriteLoop(IrExpression condition);
    protected abstract void WriteSwitch(PreparedSwitch dispatch);
    protected abstract void OpenBlock();
    protected abstract void CloseBlock();
}