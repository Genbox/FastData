using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

// return GetLength(inputKey) != value;
public sealed class LengthNotEqualEarlyExit(uint value) : MethodComparisonEarlyExitBase<uint>(value, nameof(EarlyExitFunctions.GetLength))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);
}