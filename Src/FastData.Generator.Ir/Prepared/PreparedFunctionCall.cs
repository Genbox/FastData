using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A function call with its generated target name resolved.</summary>
public sealed class PreparedFunctionCall(string name, IReadOnlyList<IrExpression> arguments, IrType type) : IrExpression
{
    public IrType Type { get; } = type;
    public string Name { get; } = name;
    public IReadOnlyList<IrExpression> Arguments { get; } = arguments;
}