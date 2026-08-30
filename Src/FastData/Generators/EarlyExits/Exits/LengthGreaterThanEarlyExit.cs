using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// Length(inputKey) > Value;
/// <summary>Rejects strings whose length is greater than the configured maximum.</summary>
/// <param name="Value">The inclusive maximum accepted length.</param>
public sealed record LengthGreaterThanEarlyExit(int Value) : MethodComparisonEarlyExitBase<int>(Value, nameof(GeneratorFunctions.Length))
{
    /// <inheritdoc />
    public override ulong KeyspaceSize => (ulong)(int.MaxValue - Value);

    /// <inheritdoc />
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);

    /// <inheritdoc />
    public override bool IsWorseThan(IEarlyExit other) => (other is LengthOutOfRangeEarlyExit range && Value >= range.Max)
                                                          || (other is LengthGreaterThanEarlyExit otherExit && Value > otherExit.Value)
                                                          || (other is LengthNotEqualEarlyExit exact && Value == exact.Value);
}