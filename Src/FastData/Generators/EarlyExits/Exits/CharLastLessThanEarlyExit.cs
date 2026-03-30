using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetLastChar(inputKey) < Value;
public sealed record CharLastLessThanEarlyExit(char Value) : MethodComparisonEarlyExitBase<char>(Value, nameof(StringFunctions.GetLastChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);

    public override bool IsWorseThan(IEarlyExit other) => other is CharLastLessThanEarlyExit otherExit && Value < otherExit.Value;
}