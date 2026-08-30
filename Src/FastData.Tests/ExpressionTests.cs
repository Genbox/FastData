using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.Helpers;

namespace Genbox.FastData.Tests;

public class ExpressionTests
{
    private static readonly ParameterExpression parameter = Expression.Variable(typeof(string), "inputKey");

    private readonly AnnotatedExpr[] _expressions =
    [
        new AnnotatedExpr(new LengthLessThanEarlyExit(4).GetExpression(parameter), ExprKind.EarlyExit),
        new AnnotatedExpr(new LengthGreaterThanEarlyExit(9).GetExpression(parameter), ExprKind.EarlyExit),
        new AnnotatedExpr(new UnitAtLessThanEarlyExit('a').GetExpression(parameter), ExprKind.EarlyExit)
    ];

    [Fact]
    public async Task NoTransformsAsync()
    {
        await VerifyAsync(ExpressionHelper.Transform(_expressions, []), nameof(NoTransformsAsync));
    }

    [Fact]
    public async Task AllocationGatherTransformAsync()
    {
        IExprTransform[] transforms = [new AllocationGatherTransform()];
        await VerifyAsync(ExpressionHelper.Transform(_expressions, transforms), nameof(AllocationGatherTransformAsync));
    }

    private static async Task VerifyAsync(object obj, string name) =>
        await Verifier.Verify(obj)
                      .UseDirectory("Verify/EarlyExits")
                      .UseFileName(name)
                      .DisableDiff()
                      .ConfigureAwait(false);
}