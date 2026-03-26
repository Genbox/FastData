using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.EarlyExits.Exits;

namespace Genbox.FastData.Tests;

public class EarlyExitTests
{
    private static readonly ParameterExpression parameter = Expression.Variable(typeof(string), "inputKey");

    private readonly AnnotatedExpr[] _expressions =
    [
        new AnnotatedExpr(new LengthLessThanEarlyExit(4).GetExpression(parameter), ExprKind.EarlyExit),
        new AnnotatedExpr(new LengthGreaterThanEarlyExit(9).GetExpression(parameter), ExprKind.EarlyExit),
        new AnnotatedExpr(new CharFirstLessThanEarlyExit('a').GetExpression(parameter), ExprKind.EarlyExit),
    ];

    [Fact]
    public async Task NoTransformsAsync()
    {
        await Verify(EarlyExitManager.Transform(_expressions, []), nameof(NoTransformsAsync));
    }

    [Fact]
    public async Task AllocationGatherTransformAsync()
    {
        IExprTransform[] transforms = [new AllocationGatherTransform()];
        await Verify(EarlyExitManager.Transform(_expressions, transforms), nameof(AllocationGatherTransformAsync));
    }

    private async Task Verify(object obj, string name) =>
        await Verifier.Verify(obj)
                      .UseDirectory("Verify/EarlyExits")
                      .UseFileName(name)
                      .DisableDiff();
}