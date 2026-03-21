using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

// GetFirstChar(inputKey) != Value;
public sealed class CharFirstNotEqualEarlyExit(char value, bool ignoreCase) : MethodComparisonEarlyExitBase<char>(value, ignoreCase ? nameof(EarlyExitFunctions.GetFirstCharLower) : nameof(EarlyExitFunctions.GetFirstChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);
}