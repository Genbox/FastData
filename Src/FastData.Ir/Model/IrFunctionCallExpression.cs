using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>A value call. Each argument evaluates exactly once in an unspecified order before the callee executes.</summary>
public sealed class IrFunctionCallExpression(IrFunctionId functionId, IReadOnlyList<IrExpression> arguments) : IrExpression
{
    public IrFunctionId FunctionId { get; } = functionId;
    public IReadOnlyList<IrExpression> Arguments { get; } = arguments;
}