using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Exits;

namespace Genbox.FastData.Tests;

public class EarlyExitRangeTests
{
    [Fact]
    public void StringLengthRangeEarlyExit_KeyspaceAndWorseThan()
    {
        StringLengthRangeEarlyExit outer = new StringLengthRangeEarlyExit(2, 8);
        StringLengthRangeEarlyExit inner = new StringLengthRangeEarlyExit(3, 5);

        Assert.False(outer.IsWorseThan(inner));
        Assert.True(inner.IsWorseThan(outer));
        Assert.Equal(5UL, outer.KeyspaceSize);
    }

    [Fact]
    public void StringLengthRangeEarlyExit_ExpressionIsExclusive()
    {
        StringLengthRangeEarlyExit exit = new StringLengthRangeEarlyExit(2, 5);
        ParameterExpression parameter = Expression.Parameter(typeof(string), "s");
        Func<string, bool> func = Expression.Lambda<Func<string, bool>>(exit.GetExpression(parameter), parameter).Compile();

        Assert.False(func("ab"));
        Assert.True(func("abc"));
        Assert.True(func("abcd"));
        Assert.False(func("abcde"));
    }

    [Fact]
    public void ValueInRangeEarlyExit_KeyspaceAndWorseThan()
    {
        ValueInRangeEarlyExit<int> outer = new ValueInRangeEarlyExit<int>(10, 20);
        ValueInRangeEarlyExit<int> inner = new ValueInRangeEarlyExit<int>(12, 15);

        Assert.False(outer.IsWorseThan(inner));
        Assert.True(inner.IsWorseThan(outer));
        Assert.Equal(9UL, outer.KeyspaceSize);
    }

    [Fact]
    public void ValueInRangeEarlyExit_ExpressionIsExclusive()
    {
        ValueInRangeEarlyExit<int> exit = new ValueInRangeEarlyExit<int>(10, 20);
        ParameterExpression parameter = Expression.Parameter(typeof(int), "x");
        Func<int, bool> func = Expression.Lambda<Func<int, bool>>(exit.GetExpression(parameter), parameter).Compile();

        Assert.False(func(10));
        Assert.True(func(11));
        Assert.True(func(19));
        Assert.False(func(20));
    }
}