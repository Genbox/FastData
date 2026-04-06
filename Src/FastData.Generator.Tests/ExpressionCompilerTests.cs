using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;

namespace Genbox.FastData.Generator.Tests;

public class ExpressionCompilerTests
{
    [Fact]
    public void BinaryExpressionRendersOperators()
    {
        TypeMap map = CreateMap();
        TestCompiler compiler = new TestCompiler(map);
        ParameterExpression parameter = Expression.Parameter(typeof(int), "input");
        Expression expression = Expression.Equal(parameter, Expression.Constant(3));

        string output = compiler.GetCode(expression);

        Assert.Equal("(input == 3)", output);
    }

    [Fact]
    public void ArrayIndexRendersBrackets()
    {
        TypeMap map = CreateMap();
        TestCompiler compiler = new TestCompiler(map);
        ParameterExpression parameter = Expression.Parameter(typeof(int[]), "numbers");
        Expression expression = Expression.ArrayIndex(parameter, Expression.Constant(1));

        string output = compiler.GetCode(expression);

        Assert.Equal("numbers[1]", output);
    }

    [Fact]
    public void MethodCallRendersMemberInvocation()
    {
        TypeMap map = CreateMap();
        TestCompiler compiler = new TestCompiler(map);
        MethodInfo method = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
        Expression expression = Expression.Call(Expression.Constant("hello"), method, Expression.Constant("he"));

        string output = compiler.GetCode(expression);

        Assert.Equal("\"hello\".StartsWith(\"he\")", output);
    }

    private static TypeMap CreateMap()
    {
        ITypeDef[] defs =
        [
            new NullTypeDef("null"),
            new StringTypeDef("string"),
            new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "int.MinValue", "int.MaxValue"),
            new ObjectTypeDef((_, type) => type.Name, (_, value) => value.ToString() ?? string.Empty)
        ];

        return new TypeMap(defs, GeneratorEncoding.Utf8Bytes);
    }

    private sealed class TestCompiler(TypeMap map) : ExpressionCompiler(map);
}
