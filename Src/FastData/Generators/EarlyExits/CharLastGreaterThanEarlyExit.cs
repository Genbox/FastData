using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

// GetLastChar(inputKey) > Value;
public sealed class CharLastGreaterThanEarlyExit(char value) : MethodComparisonEarlyExitBase<char>(value, nameof(EarlyExitFunctions.GetLastChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);
}