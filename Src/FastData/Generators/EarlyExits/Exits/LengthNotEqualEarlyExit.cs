using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// return GetLength(inputKey) != value;
public sealed record LengthNotEqualEarlyExit(uint Value) : MethodComparisonEarlyExitBase<uint>(Value, nameof(EarlyExitFunctions.GetLength))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);
}