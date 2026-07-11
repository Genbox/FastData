using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Exits;

namespace Genbox.FastData.Tests;

public class EarlyExitRangeTests
{
    [Fact]
    public void LengthInRangeEarlyExit_KeyspaceAndWorseThan()
    {
        LengthInRangeEarlyExit outer = new LengthInRangeEarlyExit(2, 8);
        LengthInRangeEarlyExit inner = new LengthInRangeEarlyExit(3, 5);

        Assert.False(outer.IsWorseThan(inner));
        Assert.True(inner.IsWorseThan(outer));
        Assert.Equal(5UL, outer.KeyspaceSize);
    }

    [Fact]
    public void LengthInRangeEarlyExit_ExpressionIsExclusive()
    {
        LengthInRangeEarlyExit exit = new LengthInRangeEarlyExit(2, 5);
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

    [Fact]
    public void UnitAtInRangeEarlyExit_KeyspaceAndWorseThan()
    {
        UnitAtInRangeEarlyExit outer = new UnitAtInRangeEarlyExit('a', 'z');
        UnitAtInRangeEarlyExit inner = new UnitAtInRangeEarlyExit('m', 'p');
        UnitAtInRangeEarlyExit differentOffset = new UnitAtInRangeEarlyExit('m', 'p', -1);

        Assert.False(outer.IsWorseThan(inner));
        Assert.True(inner.IsWorseThan(outer));
        Assert.False(differentOffset.IsWorseThan(outer));
        Assert.Equal((ulong)('z' - 'a' - 1), outer.KeyspaceSize);
    }

    [Fact]
    public void UnitAtInRangeEarlyExit_ExpressionIsExclusive_FirstOffset()
    {
        UnitAtInRangeEarlyExit exit = new UnitAtInRangeEarlyExit('b', 'e');
        ParameterExpression parameter = Expression.Parameter(typeof(string), "key");
        Func<string, bool> func = Expression.Lambda<Func<string, bool>>(exit.GetExpression(parameter), parameter).Compile();

        Assert.False(func("bat"));
        Assert.True(func("cat"));
        Assert.True(func("dog"));
        Assert.False(func("emu"));
    }

    [Fact]
    public void UnitAtInRangeEarlyExit_ExpressionIsExclusive_LastOffset()
    {
        UnitAtInRangeEarlyExit exit = new UnitAtInRangeEarlyExit('b', 'e', -1);
        ParameterExpression parameter = Expression.Parameter(typeof(string), "key");
        Func<string, bool> func = Expression.Lambda<Func<string, bool>>(exit.GetExpression(parameter), parameter).Compile();

        Assert.False(func("crab"));
        Assert.True(func("bloc"));
        Assert.True(func("old"));
        Assert.False(func("apple"));
    }

    [Fact]
    public void UnitAtInRangeEarlyExit_EmptyGapAlwaysRejectsNothing()
    {
        UnitAtInRangeEarlyExit exit = new UnitAtInRangeEarlyExit('a', 'b');
        ParameterExpression parameter = Expression.Parameter(typeof(string), "key");
        Func<string, bool> func = Expression.Lambda<Func<string, bool>>(exit.GetExpression(parameter), parameter).Compile();

        Assert.Equal(0UL, exit.KeyspaceSize);
        Assert.False(func("apple"));
        Assert.False(func("banana"));
    }
}