using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetFirstChar(inputKey) != Value;
public sealed record CharFirstNotEqualEarlyExit(char Value, bool IgnoreCase) : MethodComparisonEarlyExitBase<char>(Value, IgnoreCase ? nameof(EarlyExitFunctions.GetFirstCharLower) : nameof(EarlyExitFunctions.GetFirstChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);
}