using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines a stateful transformation over annotated generator expressions.</summary>
public interface IExprTransform
{
    /// <summary>Creates state for one transformation pass.</summary>
    /// <returns>The pass-specific state.</returns>
    object CreateState();

    /// <summary>Transforms one expression and appends its replacement expressions.</summary>
    /// <param name="expr">The expression to transform.</param>
    /// <param name="state">The state created for the current pass.</param>
    /// <param name="output">The collection that receives transformed expressions.</param>
    void Transform(AnnotatedExpr expr, object state, ICollection<AnnotatedExpr> output);
}