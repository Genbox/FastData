using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Visits a consecutive group of returning equality tests without rewriting the original IR.</summary>
public sealed class SwitchDetectionVisitor(IReadOnlyList<string> variables)
{
    private readonly List<(IrLiteral Label, IrExpression Result)> _cases = [];
    private readonly HashSet<IrLiteral> _labels = [];
    private IrVariableExpression? _variable;

    /// <summary>The generated name of the common variable when at least three cases match; otherwise null.</summary>
    public string? VariableName => _cases.Count >= 3 && _variable is {} variable ? variables[variable.VariableId.Value] : null;

    public IReadOnlyList<(IrLiteral Label, IrExpression Result)> Cases => _cases;
    public IrType? Type => _cases.Count >= 3 ? _variable?.Type : null;

    /// <summary>Resets the visitor and matches a group beginning at the requested statement.</summary>
    public void Visit(IrBlock block, int start = 0)
    {
        _variable = null;
        _labels.Clear();
        _cases.Clear();
        if (block.Statements.Count - start < 3)
            return;

        for (int i = start; i < block.Statements.Count; i++)
        {
            int previous = _cases.Count;
            if (block.Statements[i] is IrIfStatement conditional)
                Match(conditional);
            if (_cases.Count == previous)
                break;
        }

        if (_cases.Count < 3)
        {
            _variable = null;
            _cases.Clear();
        }
    }

    private void Match(IrIfStatement conditional)
    {
        if (conditional.IfFalse != null || conditional.IfTrue.Statements.Count != 1 || conditional.IfTrue.Statements[0] is not IrReturnStatement result)
            return;
        if (conditional.Condition is not IrBinaryExpression
            {
                Operation: BinaryOperation.Equal,
                Left: IrVariableExpression variable,
                Right: IrLiteral label
            } || label is IrNullLiteral)
            return;
        if (!label.Type.IsIntegral && label.Type.Kind is not (IrTypeKind.Char or IrTypeKind.String))
            return;
        if (_variable != null && _variable.VariableId != variable.VariableId)
            return;
        if (!_labels.Add(label))
            return;
        _variable ??= variable;
        _cases.Add((label, result.Value));
    }
}