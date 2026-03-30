using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetLength(inputKey) > Value;
public sealed record LengthGreaterThanEarlyExit(uint Value) : MethodComparisonEarlyExitBase<uint>(Value, nameof(StringFunctions.GetLength))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);

    public override bool IsWorseThan(IEarlyExit other) => other is LengthGreaterThanEarlyExit otherExit && Value > otherExit.Value;
}