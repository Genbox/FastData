using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// UnitAt(inputKey, Offset) < Value;
/// <summary>Rejects strings whose unit at a fixed offset is less than the configured minimum.</summary>
/// <param name="Value">The inclusive minimum accepted unit value.</param>
/// <param name="Offset">The start-relative index, or a negative end-relative index.</param>
public sealed record UnitAtLessThanEarlyExit(char Value, int Offset = 0) : MethodComparisonEarlyExitBase<char>(Value, nameof(GeneratorFunctions.UnitAt))
{
    /// <inheritdoc />
    public override ulong KeyspaceSize => Value;

    /// <inheritdoc />
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);

    /// <inheritdoc />
    public override Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.UnitAt), [typeof(string), typeof(int)])!;
        return Compare(Call(methodInfo, key, Constant(Offset)), Constant((uint)Value, typeof(uint)));
    }

    /// <inheritdoc />
    public override bool IsWorseThan(IEarlyExit other) => (other is UnitAtOutOfRangeEarlyExit range && Offset == range.Offset && Value <= range.Min) || (other is UnitAtLessThanEarlyExit otherExit && Offset == otherExit.Offset && Value < otherExit.Value);
}