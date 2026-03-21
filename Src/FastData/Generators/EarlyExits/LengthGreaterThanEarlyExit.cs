using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

// GetLength(inputKey) > Value;
public sealed class LengthGreaterThanEarlyExit(uint value) : MethodComparisonEarlyExitBase<uint>(value, nameof(EarlyExitFunctions.GetLength))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);
}