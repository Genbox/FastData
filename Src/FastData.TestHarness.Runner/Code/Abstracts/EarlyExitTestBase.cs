using System.Linq.Expressions;
using Genbox.FastData.Generator;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class EarlyExitTestBase
{
    protected abstract string HarnessName { get; }
    protected abstract TestBase Harness { get; }
    protected abstract ExpressionCompiler CreateCompiler();
    protected abstract string RenderProgram(string expression, Type keyType);

    [Theory]
    [ClassData(typeof(EarlyExitExpressions))]
    public async Task EarlyExitExpression(EarlyExitVector vector)
    {
        Expression expression = vector.EarlyExit.GetExpression("key");
        ExpressionCompiler compiler = CreateCompiler();
        string source = compiler.GetCode(expression, 0);
        string id = $"{nameof(EarlyExitExpressions)}_{vector.Id}";
        await VerifyEarlyExitAsync(HarnessName, id, source);

        string program = RenderProgram(source, GetKeyType(expression));
        Assert.Equal(1, await Harness.RunProgramAsync(program, id, TestContext.Current.CancellationToken));
    }

    private static Type GetKeyType(Expression expression)
    {
        ParameterFinder finder = new ParameterFinder();
        finder.Visit(expression);

        if (finder.Parameter == null)
            throw new InvalidOperationException("No parameter found in early-exit expression.");

        return finder.Parameter.Type;
    }

    private sealed class ParameterFinder : ExpressionVisitor
    {
        public ParameterExpression? Parameter { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Parameter ??= node;
            return base.VisitParameter(node);
        }
    }
}