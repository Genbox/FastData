using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

public sealed class EarlyExitConditionTransform(ICollection<Expression> body) : IExprTransform
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