using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetLength(inputKey) > Min && GetLength(inputKey) < Max;
public sealed record StringLengthRangeEarlyExit(int Min, int Max) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(nameof(StringFunctions.GetLength), [typeof(string)])!;
        Expression length = Call(methodInfo, key);
        Expression lower = GreaterThan(length, Constant(Min));
        Expression upper = LessThan(length, Constant(Max));
        return AndAlso(lower, upper);
    }

    public bool IsWorseThan(IEarlyExit other)
    {
        if (other is not StringLengthRangeEarlyExit otherExit)
            return false;

        if (Min == otherExit.Min && Max == otherExit.Max)
            return false;

        return Min <= otherExit.Min && Max >= otherExit.Max;
    }

    public ulong KeyspaceSize
    {
        get
        {
            ulong diff = (ulong)(Max - Min);
            return diff > 1 ? diff - 1 : 0;
        }
    }
}