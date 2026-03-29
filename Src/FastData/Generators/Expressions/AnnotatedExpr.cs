using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Expressions;

public readonly record struct AnnotatedExpr(Expression Expression, ExprKind Kind)
{
    public static AnnotatedExpr Allocation(Expression expression) => new AnnotatedExpr(expression, ExprKind.Assignment);
    public static AnnotatedExpr EarlyExit(Expression expression) => new AnnotatedExpr(expression, ExprKind.EarlyExit);
}