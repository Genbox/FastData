using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetLength(inputKey) < Value;
public sealed record LengthLessThanEarlyExit(uint Value) : MethodComparisonEarlyExitBase<uint>(Value, nameof(StringFunctions.GetLength))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);
}