using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Exits;

namespace Genbox.FastData.Tests;

public class ValueBitSetEarlyExitTests
{
    [Fact]
    public void ExpressionMatchesMissingBitSet()
    {
        ValueBitSetEarlyExit<int> exit = new ValueBitSetEarlyExit<int>(10, 14, 0b1010);
        ParameterExpression parameter = Expression.Parameter(typeof(int), "x");
        Func<int, bool> func = Expression.Lambda<Func<int, bool>>(exit.GetExpression(parameter), parameter).Compile();

        Assert.False(func(9));
        Assert.False(func(10));
        Assert.True(func(11));
        Assert.False(func(12));
        Assert.True(func(13));
        Assert.False(func(14));
        Assert.False(func(15));
        Assert.Equal(2UL, exit.KeyspaceSize);
    }
}