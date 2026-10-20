using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>Expressions for advancing an enumeration cursor and reading its current element.</summary>
public sealed class PreparedEnumeration
{
    internal PreparedEnumeration(string name, IrType type, string next, string element)
    {
        Name = name;
        Type = type;
        IrVariableExpression position = Read(new IrVariableId(0, IrType.I64));
        Start = new PreparedFunctionCall(next, [InitialPosition], IrType.I64);
        Advance = new PreparedFunctionCall(next, [position], IrType.I64);
        HasCurrent = GreaterOrEqual(position, I64(0));
        Current = new PreparedFunctionCall(element, [position], type);
    }

    public string Name { get; }
    public IrType Type { get; }
    public IReadOnlyList<string> VariableNames { get; } = ["position"];
    public IrExpression InitialPosition { get; } = I64(-1);
    public IrExpression Start { get; }
    public IrExpression Advance { get; }
    public IrExpression HasCurrent { get; }
    public IrExpression Current { get; }
}