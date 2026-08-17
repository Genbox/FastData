using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Tests;

public class EarlyExitConditionTransformTests
{
    [Fact]
    public void Transform_WrapsEarlyExitInIfThen()
    {
        List<Expression> body = [Expression.Empty()];
        EarlyExitConditionTransform transform = new EarlyExitConditionTransform(body);

        AnnotatedExpr expr = AnnotatedExpr.EarlyExit(Expression.Constant(true));
        List<AnnotatedExpr> output = new List<AnnotatedExpr>();
        transform.Transform(expr, transform.CreateState(), output);
        AnnotatedExpr transformed = Assert.Single(output);

        ConditionalExpression conditional = Assert.IsType<ConditionalExpression>(transformed.Expression);
        Assert.Same(expr.Expression, conditional.Test);
        BlockExpression block = Assert.IsAssignableFrom<BlockExpression>(conditional.IfTrue);
        Assert.Single(block.Expressions);
        Assert.Same(body[0], block.Expressions[0]);
    }

    [Fact]
    public void Transform_PassesThroughNonEarlyExit()
    {
        EarlyExitConditionTransform transform = new EarlyExitConditionTransform(new List<Expression>());
        AnnotatedExpr expr = new AnnotatedExpr(Expression.Constant(1), ExprKind.Assignment);

        List<AnnotatedExpr> output = new List<AnnotatedExpr>();
        transform.Transform(expr, transform.CreateState(), output);
        AnnotatedExpr transformed = Assert.Single(output);

        Assert.Same(expr.Expression, transformed.Expression);
        Assert.Equal(expr.Kind, transformed.Kind);
    }
}