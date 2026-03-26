using System.Linq.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

public sealed class EarlyExitConditionTransform(IReadOnlyList<Expression> body) : IExprTransform
{
    public object CreateState() => new object();

    public IEnumerable<AnnotatedExpr> Transform(AnnotatedExpr expr, object state)
    {
        if (expr.Kind != ExprKind.EarlyExit)
        {
            yield return expr;
            yield break;
        }

        yield return AnnotatedExpr.EarlyExit(IfThen(expr.Expression, Block(body)));
    }
}