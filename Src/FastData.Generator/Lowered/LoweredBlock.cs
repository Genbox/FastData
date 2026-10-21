using System.Linq.Expressions;

namespace Genbox.FastData.Generator.Lowered;

/// <summary>A scope containing local declarations and ordered statements.</summary>
public sealed class LoweredBlock(IReadOnlyList<ParameterExpression> locals, IReadOnlyList<LoweredStatement> statements) : LoweredStatement
{
    /// <summary>Gets the local variables declared in this scope.</summary>
    public IReadOnlyList<ParameterExpression> Locals { get; } = locals;

    /// <summary>Gets the statements in execution order.</summary>
    public IReadOnlyList<LoweredStatement> Statements { get; } = statements;
}