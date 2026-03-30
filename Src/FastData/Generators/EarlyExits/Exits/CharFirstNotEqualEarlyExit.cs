using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetFirstChar(inputKey) != Value;
public sealed record CharFirstNotEqualEarlyExit(char Value, bool IgnoreCase) : MethodComparisonEarlyExitBase<char>(Value, IgnoreCase ? nameof(StringFunctions.GetFirstCharLower) : nameof(StringFunctions.GetFirstChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    public override bool IsWorseThan(IEarlyExit other) => false;
}