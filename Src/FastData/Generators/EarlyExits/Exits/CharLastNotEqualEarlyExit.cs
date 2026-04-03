using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetLastChar(inputKey) != Value;
public sealed record CharLastNotEqualEarlyExit(char Value, bool IgnoreCase) : MethodComparisonEarlyExitBase<char>(Value, IgnoreCase ? nameof(StringFunctions.GetLastCharLower) : nameof(StringFunctions.GetLastChar))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    public override bool IsWorseThan(IEarlyExit other) => false;

    public override ulong KeyspaceSize => 1;
}