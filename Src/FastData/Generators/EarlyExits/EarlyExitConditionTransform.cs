using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

public sealed class EarlyExitConditionTransform(ICollection<Expression> body) : IExprTransform
{
    public object CreateState() => this;

    public void Transform(AnnotatedExpr expr, object state, List<AnnotatedExpr> output)
    {
        if (expr.Kind != ExprKind.EarlyExit)
        {
            output.Add(expr);
            return;
        }

        output.Add(AnnotatedExpr.EarlyExit(IfThen(expr.Expression, Block(body))));
    }
}