using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

/// <summary>
/// Rejects strings whose length falls outside the observed [Min, Max] range using a single unsigned subtraction check:
/// <c>(uint)(Length(key) - Min) &gt; (uint)(Max - Min)</c>.
/// </summary>
public sealed record LengthOutOfRangeEarlyExit(int Min, int Max) : IEarlyExit
{
    public ulong KeyspaceSize => (ulong)Min + (ulong)(int.MaxValue - Max);

    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        Expression length = Call(methodInfo, key);

        // (uint)(Length(key) - Min) > (uint)(Max - Min)
        Expression diff = Convert(Subtract(length, Constant(Min)), typeof(uint));
        uint rangeVal = unchecked((uint)(Max - Min));
        Expression range = Constant(rangeVal);

        return GreaterThan(diff, range);
    }

    public bool IsWorseThan(IEarlyExit other) => false;
}