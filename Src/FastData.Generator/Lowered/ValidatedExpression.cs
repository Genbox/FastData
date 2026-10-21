using System.Linq.Expressions;

namespace Genbox.FastData.Generator.Lowered;

/// <summary>An expression checked by the shared expression lowerer.</summary>
public sealed class ValidatedExpression(Expression source, ISet<ParameterExpression> localReads)
{
    /// <summary>Gets the source expression.</summary>
    public Expression Source { get; } = source;

    /// <summary>Gets the result type of the expression.</summary>
    public Type Type => Source.Type;

    internal ISet<ParameterExpression> LocalReads { get; } = localReads;
}