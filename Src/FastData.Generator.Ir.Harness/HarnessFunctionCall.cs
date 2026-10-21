using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Harness;

internal sealed class HarnessFunctionCall(string name, IrType type, IReadOnlyList<IrExpression> arguments) : IrExtensionExpression
{
    public string Name { get; } = name;
    public IrType Type { get; } = type;
    public IReadOnlyList<IrExpression> Children { get; } = arguments;
}