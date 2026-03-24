using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetLastChar(inputKey) != Value;
public sealed class CharLastNotEqualEarlyExit(char value, bool ignoreCase) : MethodComparisonEarlyExitBase<char>(value, ignoreCase ? nameof(EarlyExitFunctions.GetLastCharLower) : nameof(EarlyExitFunctions.GetLastChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);
}