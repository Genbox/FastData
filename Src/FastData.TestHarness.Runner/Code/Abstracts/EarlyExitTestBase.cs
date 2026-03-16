using System.Linq.Expressions;
using Genbox.FastData.Generator;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class EarlyExitTestBase
{
    protected abstract string HarnessName { get; }
    protected abstract ExpressionCompiler CreateCompiler();

    [Theory]
    [ClassData(typeof(EarlyExitExpressions))]
    public Task EarlyExitExpression(EarlyExitVector vector) => VerifyAsync(vector);

    private Task VerifyAsync(EarlyExitVector vector)
    {
        string source = Compile(vector.EarlyExit);
        string id = $"{nameof(EarlyExitExpressions)}_{vector.Id}";
        return VerifyEarlyExitAsync(HarnessName, id, source);
    }

    private string Compile(IEarlyExit earlyExit)
    {
        Expression expression = earlyExit.GetExpression("key");
        ExpressionCompiler compiler = CreateCompiler();
        return compiler.GetCode(expression, 0);
    }
}