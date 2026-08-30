using System;
using System.Linq;
using System.Linq.Expressions;
using Genbox.FastData.Generator;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.TestHarness.Runner.Tests;

public sealed class LoweredProgramRenderingTests
{
    private const string CSharpTarget = "CSharp";
    private const string CPlusPlusTarget = "CPlusPlus";
    private const string RustTarget = "Rust";

    [Theory]
    [InlineData(CSharpTarget, "int first = 0;", "int second;")]
    [InlineData(CPlusPlusTarget, "int32_t first = 0;", "int32_t second;")]
    [InlineData(RustTarget, "let mut first: i32 = 0;", "let mut second: i32;")]
    public void ReadBeforeWriteGetsOnlyRequiredTargetDefault(string target, string firstDeclaration, string secondDeclaration)
    {
        ExpressionCompiler compiler = CreateCompiler(target);
        Expression<Func<int, int>> expression = CreateReadBeforeWrite();

        string[] lines = GetSignificantLines(compiler.GetLambdaBody(expression));

        Assert.Contains(firstDeclaration, lines, StringComparer.Ordinal);
        Assert.Contains(secondDeclaration, lines, StringComparer.Ordinal);
        Assert.Contains("second = first;", lines, StringComparer.Ordinal);
        Assert.Equal(1, lines.Count(static line => line.EndsWith(" = 0;", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(CSharpTarget, "int inner;")]
    [InlineData(CPlusPlusTarget, "int32_t inner;")]
    [InlineData(RustTarget, "let mut inner: i32;")]
    public void TerminalBlockWithLocalsRetainsScope(string target, string declaration)
    {
        ExpressionCompiler compiler = CreateCompiler(target);
        Expression<Func<int, int>> expression = CreateTerminalScopedBlock();

        string[] lines = GetSignificantLines(compiler.GetLambdaBody(expression));

        Assert.Contains(declaration, lines, StringComparer.Ordinal);
        Assert.Contains("inner = input;", lines, StringComparer.Ordinal);
        Assert.Equal(1, CountLine(lines, "{"));
        Assert.Equal(1, CountLine(lines, "}"));
        Assert.Equal(1, CountLine(lines, "return inner;"));
        Assert.DoesNotContain("inner;", lines, StringComparer.Ordinal);
        Assert.DoesNotContain(";", lines, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(CSharpTarget, "value = default;")]
    [InlineData(CPlusPlusTarget, "value = nullptr;")]
    [InlineData(RustTarget, "value = None;")]
    public void TargetDefaultAndReturnAreSemanticNodes(string target, string assignment)
    {
        ExpressionCompiler compiler = CreateCompiler(target);
        BlockExpression expression = CreateTargetDefaultReturnFragment();

        string[] lines = GetSignificantLines(compiler.GetStatements(expression));

        Assert.Equal([assignment, "return false;"], lines);
    }

    private static ExpressionCompiler CreateCompiler(string target) => target switch
    {
        CSharpTarget => new CSharpBootstrap(HarnessType.Test).CreateExpressionCompiler(),
        CPlusPlusTarget => new CPlusPlusBootstrap(HarnessType.Test).CreateExpressionCompiler(),
        RustTarget => new RustBootstrap(HarnessType.Test).CreateExpressionCompiler(),
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown rendering target.")
    };

    private static Expression<Func<int, int>> CreateReadBeforeWrite()
    {
        ParameterExpression input = Expression.Parameter(typeof(int), "input");
        ParameterExpression first = Expression.Variable(typeof(int), "first");
        ParameterExpression second = Expression.Variable(typeof(int), "second");
        BlockExpression body = Expression.Block(
            [first, second],
            Expression.Assign(second, first),
            Expression.Assign(first, input),
            second);
        return Expression.Lambda<Func<int, int>>(body, input);
    }

    private static Expression<Func<int, int>> CreateTerminalScopedBlock()
    {
        ParameterExpression input = Expression.Parameter(typeof(int), "input");
        ParameterExpression inner = Expression.Variable(typeof(int), "inner");
        BlockExpression tail = Expression.Block(
            [inner],
            Expression.Assign(inner, input),
            inner);
        return Expression.Lambda<Func<int, int>>(Expression.Block(tail), input);
    }

    private static BlockExpression CreateTargetDefaultReturnFragment()
    {
        ParameterExpression value = Expression.Parameter(typeof(object), "value");
        ConstantExpression result = Expression.Constant(false);
        LabelTarget target = Expression.Label(typeof(bool));
        GotoExpression returnStatement = Expression.MakeGoto(GotoExpressionKind.Return, target, result, typeof(bool));
        return Expression.Block(Expression.Assign(value, Expression.Default(typeof(object))), returnStatement);
    }

    private static string[] GetSignificantLines(string source) => source.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static int CountLine(string[] lines, string expected)
    {
        int count = 0;
        foreach (string line in lines)
        {
            if (string.Equals(line, expected, StringComparison.Ordinal))
                count++;
        }

        return count;
    }
}