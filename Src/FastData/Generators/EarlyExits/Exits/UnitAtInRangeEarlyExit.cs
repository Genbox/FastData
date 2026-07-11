using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

/// <summary>
/// Rejects strings whose character at the given offset falls strictly inside a missing interior range: values in
/// <c>(Min, Max)</c> exclusive were never observed, using a single unsigned subtraction check.
/// </summary>
/// <remarks>Since <see cref="GeneratorFunctions.UnitAt" /> returns <see cref="uint" />, the subtraction is naturally unsigned and no cast is needed.</remarks>
// UnitAt(inputKey, Offset) > Min && UnitAt(inputKey, Offset) < Max;
public sealed record UnitAtInRangeEarlyExit(char Min, char Max, int Offset = 0) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        if (KeyspaceSize == 0)
            return Constant(false);

        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.UnitAt), [typeof(string), typeof(int)])!;
        Expression charValue = Call(methodInfo, key, Constant(Offset));

        // UnitAt returns uint, so subtraction is naturally unsigned.
        Expression diff = Subtract(charValue, Constant((uint)(Min + 1), typeof(uint)));
        uint range = unchecked((uint)(Max - Min - 2));
        return LessThanOrEqual(diff, Constant(range, typeof(uint)));
    }

    public bool IsWorseThan(IEarlyExit other)
    {
        if (other is not UnitAtInRangeEarlyExit otherExit || Offset != otherExit.Offset)
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
