using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Preparation;

internal sealed class FunctionPreparation(IrProgram program, IrBackendCapabilities capabilities, string namePrefix)
{
    public PreparedFunctionGroup Run(IReadOnlyDictionary<IrBuiltinOperation, IrFunctionId>? entryPoints = null)
    {
        IReadOnlyList<IrCallable> callables = program.Callables;
        IReadOnlyList<string> names = PreparedNames.Create(namePrefix, callables.Count);
        List<PreparedFunction> functions = new List<PreparedFunction>(callables.Count);
        IrBuiltinOperation requiredBuiltins = IrBuiltinOperation.None;

        for (int i = 0; i < callables.Count; i++)
        {
            if (callables[i] is IrFunction function)
            {
                ExpressionPreparation expressions = new ExpressionPreparation(program, function, capabilities, names);
                IrBlock body = PrepareBlock(function.Body, expressions);
                functions.Add(new PreparedFunction(names[i], function, body));
            }
            else if (callables[i] is IrExternalFunction external)
            {
                // Lowering declares builtins on demand, including dependencies of fallback functions.
                if (!IrBuiltins.TryGetOperation(external.Symbol, out IrBuiltinOperation operation))
                    throw new NotSupportedException($"Unknown external function '{external.Symbol}'.");
                requiredBuiltins |= operation;
            }
        }

        return new PreparedFunctionGroup(functions.ToArray(), names, requiredBuiltins, entryPoints);
    }

    private IrBlock PrepareBlock(IrBlock block, ExpressionPreparation expressions)
    {
        List<IrStatement>? statements = null;

        for (int i = 0; i < block.Statements.Count; i++)
        {
            PreparedSwitch? dispatch = CreateSwitch(block.Statements, i, expressions);
            IrStatement statement = dispatch ?? PrepareStatement(block.Statements[i], expressions);

            if (statements == null && !ReferenceEquals(statement, block.Statements[i]))
            {
                statements = new List<IrStatement>(block.Statements.Count);
                for (int j = 0; j < i; j++)
                    statements.Add(block.Statements[j]);
            }

            statements?.Add(statement);
            if (dispatch != null)
                i += dispatch.Cases.Count - 1;
        }

        return statements == null ? block : new IrBlock(statements);
    }

    private IrStatement PrepareStatement(IrStatement statement, ExpressionPreparation expressions)
    {
        switch (statement)
        {
            case IrIfStatement conditional:
            {
                IrExpression condition = expressions.Prepare(conditional.Condition);
                IrBlock ifTrue = PrepareBlock(conditional.IfTrue, expressions);
                IrBlock? ifFalse = conditional.IfFalse == null ? null : PrepareBlock(conditional.IfFalse, expressions);
                return ReferenceEquals(condition, conditional.Condition) && ReferenceEquals(ifTrue, conditional.IfTrue) && ReferenceEquals(ifFalse, conditional.IfFalse)
                    ? conditional
                    : new IrIfStatement(condition, ifTrue, ifFalse);
            }
            case IrLoopStatement loop:
            {
                IrExpression condition = expressions.Prepare(loop.Condition);
                IrBlock body = PrepareBlock(loop.Body, expressions);
                return ReferenceEquals(condition, loop.Condition) && ReferenceEquals(body, loop.Body) ? loop : new IrLoopStatement(condition, body);
            }
            case IrAssignmentStatement assignment:
            {
                IrExpression value = expressions.Prepare(assignment.Value);
                return ReferenceEquals(value, assignment.Value) ? assignment : new IrAssignmentStatement(assignment.VariableId, value);
            }
            case IrReturnStatement result:
            {
                IrExpression value = expressions.Prepare(result.Value);
                return ReferenceEquals(value, result.Value) ? result : new IrReturnStatement(value);
            }
            default:
                return statement;
        }
    }

    private PreparedSwitch? CreateSwitch(IReadOnlyList<IrStatement> statements, int start, ExpressionPreparation expressions)
    {
        if (capabilities.SwitchKinds == IrSwitchKinds.None || statements.Count - start < 3 || MatchCase(statements[start]) is not {} first)
            return null;

        List<PreparedSwitchCase> cases = [new PreparedSwitchCase(first.Label, first.Result)];
        HashSet<IrLiteral> labels = [first.Label];

        for (int i = start + 1; i < statements.Count; i++)
        {
            if (MatchCase(statements[i]) is not {} next || next.Variable.VariableId != first.Variable.VariableId || !labels.Add(next.Label))
                break;
            cases.Add(new PreparedSwitchCase(next.Label, next.Result));
        }

        if (cases.Count < 3)
            return null;

        for (int i = 0; i < cases.Count; i++)
            cases[i] = new PreparedSwitchCase(cases[i].Label, expressions.Prepare(cases[i].Result));
        return new PreparedSwitch(first.Variable, cases.ToArray());
    }

    private (IrVariableExpression Variable, IrLiteral Label, IrExpression Result)? MatchCase(IrStatement statement)
    {
        if (statement is not IrIfStatement { IfFalse: null } conditional || conditional.IfTrue.Statements.Count != 1)
            return null;

        if (conditional.Condition is not IrBinaryExpression
            {
                Operation: BinaryOperation.Equal,
                Left: IrVariableExpression variable,
                Right: IrLiteral label
            } || label is IrNullLiteral)
            return null;

        if (conditional.IfTrue.Statements[0] is not IrReturnStatement result)
            return null;

        IrSwitchKinds kind;

        if (label.Type.IsIntegral)
            kind = IrSwitchKinds.Integral;
        else
        {
            kind = label.Type.Kind switch
            {
                IrTypeKind.Char => IrSwitchKinds.Character,
                IrTypeKind.String => IrSwitchKinds.String,
                _ => IrSwitchKinds.None
            };
        }

        return kind != IrSwitchKinds.None && capabilities.SwitchKinds.HasFlag(kind) ? (variable, label, result.Value) : null;
    }
}