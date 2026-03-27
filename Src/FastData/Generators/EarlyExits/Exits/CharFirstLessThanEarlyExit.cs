using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetFirstChar(inputKey) < Value;
public sealed record CharFirstLessThanEarlyExit(char Value) : MethodComparisonEarlyExitBase<char>(Value, nameof(StringFunctions.GetFirstChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);
}