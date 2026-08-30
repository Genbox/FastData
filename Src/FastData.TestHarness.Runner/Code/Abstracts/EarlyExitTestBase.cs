using System.Linq.Expressions;
using Genbox.FastData.Generator;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class EarlyExitTestBase
{
    private readonly TestBase _testBase;
    private readonly ExpressionCompiler _compiler;

    private protected EarlyExitTestBase(TestBase testBase, ExpressionCompiler compiler)
    {
        _testBase = testBase;
        _compiler = compiler;
    }

    protected abstract string RenderProgram(string expression, object value);

    [Theory]
    [ClassData(typeof(EarlyExitVectors))]
    public async Task EarlyExitTest(EarlyExitVector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        ParameterExpression variable = Expression.Variable(vector.Match.GetType(), "inputKey");
        Expression expression = vector.EarlyExit.GetExpression(variable);
        string source = _compiler.GetCode(expression);
        await VerifyEarlyExitAsync(_testBase.Name, vector.SnapshotId, source);

        string matchProgram = RenderProgram(source, vector.Match);
        Assert.Equal(1, await _testBase.RunProgramAsync(matchProgram, vector.ProgramId + "_match", true, TestContext.Current.CancellationToken));

        string noMatchProgram = RenderProgram(source, vector.NoMatch);
        Assert.Equal(0, await _testBase.RunProgramAsync(noMatchProgram, vector.ProgramId + "_nomatch", true, TestContext.Current.CancellationToken));
    }
}