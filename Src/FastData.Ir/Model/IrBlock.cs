using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>An ordered statement list used as a function, if, or loop body. Variables use function-wide scope.</summary>
/// <remarks>Takes ownership of the statement collection without copying it. Callers must not modify it through any alias; immutable statements may be shared.</remarks>
public sealed class IrBlock(IReadOnlyList<IrStatement> statements)
{
    public IReadOnlyList<IrStatement> Statements { get; } = statements;
}