using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// return GetLength(inputKey) != value;
public sealed record LengthNotEqualEarlyExit(int Value) : MethodComparisonEarlyExitBase<int>(Value, nameof(StringFunctions.GetLength))
{
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    public override bool IsWorseThan(IEarlyExit other) => false;

    public override ulong KeyspaceSize => 1;
}