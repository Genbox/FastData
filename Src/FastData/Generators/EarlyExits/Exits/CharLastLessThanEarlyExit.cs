using System.Linq.Expressions;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetLastChar(inputKey) < Value;
public sealed record CharLastLessThanEarlyExit(char Value) : MethodComparisonEarlyExitBase<char>(Value, nameof(EarlyExitFunctions.GetLastChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);
}