using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Harness;

internal sealed class HarnessCall(HarnessOperation operation, IrType type, params IrExpression[] arguments) : IrExtensionExpression
{
    public HarnessOperation Operation { get; } = operation;
    public IrType Type { get; } = type;
    public IReadOnlyList<IrExpression> Children { get; } = arguments;
}