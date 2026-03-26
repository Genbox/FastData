using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetFirstChar(inputKey) > Value;
public sealed record CharFirstGreaterThanEarlyExit(char Value) : MethodComparisonEarlyExitBase<char>(Value, nameof(EarlyExitFunctions.GetFirstChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);
}