using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

/// <summary>Rejects strings whose character at the given offset falls outside the observed [Min, Max] range using a single
/// unsigned subtraction check: <c>UnitAt(key, offset) - Min &gt; Max - Min</c>.</summary>
/// <remarks>Since <see cref="GeneratorFunctions.UnitAt" /> returns <see cref="uint" />, the subtraction is naturally unsigned and no cast is needed.</remarks>
public sealed record UnitAtRangeEarlyExit(char Min, char Max, int Offset = 0) : IEarlyExit
{
    public ulong KeyspaceSize => (ulong)Min + (ulong)(char.MaxValue - Max);

    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.UnitAt), [typeof(string), typeof(int)])!;
        Expression charValue = Call(methodInfo, key, Constant(Offset));

        // UnitAt returns uint, so subtraction is naturally unsigned.
        // UnitAt(key, offset) - Min > Max - Min
        Expression diff = Subtract(charValue, Constant((uint)Min, typeof(uint)));
        uint rangeVal = unchecked((uint)(Max - Min));
        Expression range = Constant(rangeVal, typeof(uint));

        return GreaterThan(diff, range);
    }

    public bool IsWorseThan(IEarlyExit other) => false;
}