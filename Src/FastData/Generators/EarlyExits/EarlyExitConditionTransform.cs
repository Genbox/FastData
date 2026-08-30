using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Wraps early-exit conditions in conditional blocks containing the configured exit body.</summary>
/// <param name="body">The expressions to execute when an early-exit condition is met.</param>
public sealed class EarlyExitConditionTransform(ICollection<Expression> body) : IExprTransform
{
    /// <inheritdoc />
    public object CreateState() => this;

    /// <inheritdoc />
    public void Transform(AnnotatedExpr expr, object state, ICollection<AnnotatedExpr> output)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        if (expr.Kind != ExprKind.EarlyExit)
        {
            output.Add(expr);
            return;
        }

        output.Add(AnnotatedExpr.EarlyExit(IfThen(expr.Expression, Block(body))));
    }
}