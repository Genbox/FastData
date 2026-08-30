using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// Length(inputKey) < Value;
/// <summary>Rejects strings whose length is less than the configured minimum.</summary>
/// <param name="Value">The inclusive minimum accepted length.</param>
public sealed record LengthLessThanEarlyExit(int Value) : MethodComparisonEarlyExitBase<int>(Value, nameof(GeneratorFunctions.Length))
{
    /// <inheritdoc />
    public override ulong KeyspaceSize => (ulong)Value;

    /// <inheritdoc />
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);

    /// <inheritdoc />
    public override bool IsWorseThan(IEarlyExit other) => (other is LengthOutOfRangeEarlyExit range && Value <= range.Min) || (other is LengthLessThanEarlyExit otherExit && Value < otherExit.Value) || (other is LengthNotEqualEarlyExit exact && Value == exact.Value);
}