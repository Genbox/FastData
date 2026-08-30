using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Expressions;

/// <summary>Associates an expression with its role in the generated method.</summary>
/// <param name="Expression">The expression.</param>
/// <param name="Kind">The expression role.</param>
public readonly record struct AnnotatedExpr(Expression Expression, ExprKind Kind)
{
    /// <summary>Creates an allocation or assignment expression annotation.</summary>
    /// <param name="expression">The expression to annotate.</param>
    /// <returns>The annotated expression.</returns>
    public static AnnotatedExpr Allocation(Expression expression) => new AnnotatedExpr(expression, ExprKind.Assignment);

    /// <summary>Creates an early-exit expression annotation.</summary>
    /// <param name="expression">The expression to annotate.</param>
    /// <returns>The annotated expression.</returns>
    public static AnnotatedExpr EarlyExit(Expression expression) => new AnnotatedExpr(expression, ExprKind.EarlyExit);
}