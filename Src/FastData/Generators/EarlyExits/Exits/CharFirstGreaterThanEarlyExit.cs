using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetFirstChar(inputKey) > Value;
public sealed class CharFirstGreaterThanEarlyExit(char value) : MethodComparisonEarlyExitBase<char>(value, nameof(EarlyExitFunctions.GetFirstChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);
}