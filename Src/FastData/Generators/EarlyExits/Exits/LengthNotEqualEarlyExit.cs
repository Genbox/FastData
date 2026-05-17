using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// return Length(inputKey) != value;
public sealed record LengthNotEqualEarlyExit(int Value) : MethodComparisonEarlyExitBase<int>(Value, nameof(GeneratorFunctions.Length))
{
    public override ulong KeyspaceSize => 1;
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    public override bool IsWorseThan(IEarlyExit other) => false;
}