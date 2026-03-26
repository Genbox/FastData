using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
using static Genbox.FastData.Generators.Expressions.Optimizer.ExprOptimizer;

namespace Genbox.FastData.Tests;

public sealed class ExprOptimizerEdgeCaseTests
{
    [Fact]
    public void AddCheckedOverflowDoesNotFold()
    {
        BinaryExpression expression = AddChecked(Constant(int.MaxValue), Constant(1));

        Expression optimized = Visit(expression);

        BinaryExpression result = Assert.IsType<BinaryExpression>(optimized, exactMatch: false);
        Assert.Equal(ExpressionType.AddChecked, result.NodeType);
        ConstantExpression left = Assert.IsType<ConstantExpression>(result.Left);
        ConstantExpression right = Assert.IsType<ConstantExpression>(result.Right);
        Assert.Equal(int.MaxValue, (int)left.Value!);
        Assert.Equal(1, (int)right.Value!);
    }

    [Fact]
    public void DivideByZeroConstantDoesNotFold()
    {
        BinaryExpression expression = Divide(Constant(1), Constant(0));

        Expression optimized = Visit(expression);

        BinaryExpression result = Assert.IsType<BinaryExpression>(optimized, exactMatch: false);
        Assert.Equal(ExpressionType.Divide, result.NodeType);
        ConstantExpression right = Assert.IsType<ConstantExpression>(result.Right);
        Assert.Equal(0, (int)right.Value!);
    }

    [Fact]
    public void ModuloByZeroConstantDoesNotFold()
    {
        BinaryExpression expression = Modulo(Constant(1), Constant(0));

        Expression optimized = Visit(expression);

        BinaryExpression result = Assert.IsType<BinaryExpression>(optimized, exactMatch: false);
        Assert.Equal(ExpressionType.Modulo, result.NodeType);
        ConstantExpression right = Assert.IsType<ConstantExpression>(result.Right);
        Assert.Equal(0, (int)right.Value!);
    }

    [Fact]
    public void NaNEqualityFoldsToFalse()
    {
        BinaryExpression expression = Equal(Constant(double.NaN), Constant(double.NaN));

        Expression optimized = Visit(expression);

        ConstantExpression result = Assert.IsType<ConstantExpression>(optimized, exactMatch: false);
        Assert.Equal(typeof(bool), result.Type);
        Assert.False((bool)result.Value!);
    }

    [Fact]
    public void FloatSubtractSameExpressionIsNotFolded()
    {
        ParameterExpression parameter = Parameter(typeof(float), "x");
        BinaryExpression expression = Subtract(parameter, parameter);

        Expression optimized = Visit(expression);

        BinaryExpression result = Assert.IsType<BinaryExpression>(optimized, exactMatch: false);
        Assert.Equal(ExpressionType.Subtract, result.NodeType);
        Assert.Equal(expression.ToString(), result.ToString());
    }

    [Fact]
    public void FloatMultiplyByZeroIsNotFolded()
    {
        ParameterExpression parameter = Parameter(typeof(float), "x");
        BinaryExpression expression = Multiply(parameter, Constant(0f));

        Expression optimized = Visit(expression);

        BinaryExpression result = Assert.IsType<BinaryExpression>(optimized, exactMatch: false);
        Assert.Equal(ExpressionType.Multiply, result.NodeType);
        ConstantExpression right = Assert.IsType<ConstantExpression>(result.Right);
        Assert.Equal(0f, (float)right.Value!);
    }

    [Fact]
    public void FloatEqualityWithItselfIsNotFolded()
    {
        ParameterExpression parameter = Parameter(typeof(float), "x");
        BinaryExpression expression = Equal(parameter, parameter);

        Expression optimized = Visit(expression);

        BinaryExpression result = Assert.IsType<BinaryExpression>(optimized, exactMatch: false);
        Assert.Equal(ExpressionType.Equal, result.NodeType);
        Assert.Equal(expression.ToString(), result.ToString());
    }

    [Fact]
    public void NestedAddWithLeftConstantPreservesParameter()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Add(Add(Constant(2), parameter), Constant(3));

        Expression optimized = Visit(expression);

        Func<int, int> func = Lambda<Func<int, int>>(optimized, parameter).Compile();
        Assert.Equal(15, func(10));
    }

    [Fact]
    public void NestedAddWithNonAddInnerPreservesSemantics()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Add(Subtract(parameter, Constant(2)), Constant(3));

        Expression optimized = Visit(expression);

        Func<int, int> func = Lambda<Func<int, int>>(optimized, parameter).Compile();
        Assert.Equal(11, func(10));
    }

    [Fact]
    public void NestedSubtractWithNonSubtractInnerPreservesSemantics()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Subtract(Add(parameter, Constant(2)), Constant(3));

        Expression optimized = Visit(expression);

        Func<int, int> func = Lambda<Func<int, int>>(optimized, parameter).Compile();
        Assert.Equal(9, func(10));
    }

    [Fact]
    public void NestedMultiplyWithNonMultiplyInnerPreservesSemantics()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(Add(parameter, Constant(2)), Constant(3));

        Expression optimized = Visit(expression);

        Func<int, int> func = Lambda<Func<int, int>>(optimized, parameter).Compile();
        Assert.Equal(36, func(10));
    }

    [Fact]
    public void NestedMultiplyWithLeftConstantPreservesParameter()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Multiply(Multiply(Constant(2), parameter), Constant(3));

        Expression optimized = Visit(expression);

        Func<int, int> func = Lambda<Func<int, int>>(optimized, parameter).Compile();
        Assert.Equal(60, func(10));
    }

    [Fact]
    public void SubtractWithSameNameParametersIsNotSimplified()
    {
        ParameterExpression left = Parameter(typeof(int), "x");
        ParameterExpression right = Parameter(typeof(int), "x");
        BinaryExpression expression = Subtract(left, right);

        Expression optimized = Visit(expression);

        Func<int, int, int> original = Lambda<Func<int, int, int>>(expression, left, right).Compile();
        Func<int, int, int> actual = Lambda<Func<int, int, int>>(optimized, left, right).Compile();
        Assert.Equal(original(5, 3), actual(5, 3));
    }

    [Fact]
    public void ConditionalWithSameNameParametersIsNotSimplified()
    {
        ParameterExpression test = Parameter(typeof(bool), "t");
        ParameterExpression left = Parameter(typeof(int), "x");
        ParameterExpression right = Parameter(typeof(int), "x");
        ConditionalExpression expression = Condition(test, left, right);

        Expression optimized = Visit(expression);

        Func<bool, int, int, int> original = Lambda<Func<bool, int, int, int>>(expression, test, left, right).Compile();
        Func<bool, int, int, int> actual = Lambda<Func<bool, int, int, int>>(optimized, test, left, right).Compile();
        Assert.Equal(original(false, 1, 2), actual(false, 1, 2));
    }

    [Fact]
    public void AdditiveIdentityForDoublePreservesSignedZero()
    {
        ParameterExpression parameter = Parameter(typeof(double), "x");
        BinaryExpression expression = Add(parameter, Constant(0d));

        Expression optimized = Visit(expression);

        Func<double, double> original = Lambda<Func<double, double>>(expression, parameter).Compile();
        Func<double, double> actual = Lambda<Func<double, double>>(optimized, parameter).Compile();
        double input = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000));
        double expectedValue = original(input);
        double actualValue = actual(input);
        Assert.Equal(BitConverter.DoubleToInt64Bits(expectedValue), BitConverter.DoubleToInt64Bits(actualValue));
    }

    [Fact]
    public void NestedAddConstantOverflowDoesNotThrow()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        BinaryExpression expression = Add(Add(parameter, Constant(int.MaxValue)), Constant(1));

        Exception? exception = Record.Exception(() => Visit(expression));

        Assert.Null(exception);
    }

    [Fact]
    public void ConditionalWithConstantTrueReturnsTrueBranch()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        ConditionalExpression expression = Condition(Constant(true), parameter, Constant(1));

        Expression optimized = Visit(expression);

        ParameterExpression result = Assert.IsType<ParameterExpression>(optimized, exactMatch: false);
        Assert.Same(parameter, result);
    }

    [Fact]
    public void ConditionalWithConstantFalseReturnsFalseBranch()
    {
        ParameterExpression parameter = Parameter(typeof(int), "x");
        ConditionalExpression expression = Condition(Constant(false), Constant(1), parameter);

        Expression optimized = Visit(expression);

        ParameterExpression result = Assert.IsType<ParameterExpression>(optimized, exactMatch: false);
        Assert.Same(parameter, result);
    }

    [Fact]
    public void NotFalseFoldsToTrue()
    {
        UnaryExpression expression = Not(Constant(false));

        Expression optimized = Visit(expression);

        ConstantExpression result = Assert.IsType<ConstantExpression>(optimized, exactMatch: false);
        Assert.True((bool)result.Value!);
    }

    [Fact]
    public void NotTrueFoldsToFalse()
    {
        UnaryExpression expression = Not(Constant(true));

        Expression optimized = Visit(expression);

        ConstantExpression result = Assert.IsType<ConstantExpression>(optimized, exactMatch: false);
        Assert.False((bool)result.Value!);
    }

    [Fact]
    public void MemberAccessOnConstantInstanceFoldsToConstant()
    {
        ConstantHolder holder = new ConstantHolder(13);
        MemberExpression expression = Property(Constant(holder), nameof(ConstantHolder.Value));

        Expression optimized = Visit(expression);

        ConstantExpression result = Assert.IsType<ConstantExpression>(optimized, exactMatch: false);
        Assert.Equal(13, (int)result.Value!);
    }

    private sealed class ConstantHolder(int value)
    {
        public int Value { get; } = value;
    }
}
