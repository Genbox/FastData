using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A native builtin call resolved during target preparation.</summary>
public sealed class PreparedBuiltinCall(IrBuiltinOperation operation, IReadOnlyList<IrExpression> arguments, IrType type) : IrExpression
{
    public IrType Type { get; } = type;
    public IrBuiltinOperation Operation { get; } = operation;
    public IReadOnlyList<IrExpression> Arguments { get; } = arguments;
}