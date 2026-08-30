using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

/// <summary>
/// Rejects strings whose character at the given offset falls outside the observed [Min, Max] range using a single
/// unsigned subtraction check: <c>UnitAt(key, offset) - Min &gt; Max - Min</c>.
/// </summary>
/// <remarks>Since <see cref="GeneratorFunctions.UnitAt" /> returns <see cref="uint" />, the subtraction is naturally unsigned and no cast is needed.</remarks>
/// <param name="Min">The inclusive minimum accepted unit value.</param>
/// <param name="Max">The inclusive maximum accepted unit value.</param>
/// <param name="Offset">The start-relative index, or a negative end-relative index.</param>
public sealed record UnitAtOutOfRangeEarlyExit(char Min, char Max, int Offset = 0) : IEarlyExit
{
    /// <inheritdoc />
    public ulong KeyspaceSize => Min + (ulong)(char.MaxValue - Max);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool IsWorseThan(IEarlyExit other) => false;
}