using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// Length(inputKey) > Min && Length(inputKey) < Max;
public sealed record LengthInRangeEarlyExit(int Min, int Max) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        Expression length = Call(methodInfo, key);

        if (KeyspaceSize == 0)
            return Constant(false);

        Expression diff = Convert(Subtract(length, Constant(Min + 1)), typeof(uint));
        Expression range = Constant(unchecked((uint)(Max - Min - 2)));
        return LessThanOrEqual(diff, range);
    }

    public bool IsWorseThan(IEarlyExit other)
    {
        if (other is not LengthInRangeEarlyExit otherExit)
            return false;

        if (Min == otherExit.Min && Max == otherExit.Max)
            return false;

        return Min >= otherExit.Min && Max <= otherExit.Max;
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