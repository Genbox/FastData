using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// Length(inputKey) < Value;
public sealed record LengthLessThanEarlyExit(int Value) : MethodComparisonEarlyExitBase<int>(Value, nameof(GeneratorFunctions.Length))
{
    public override ulong KeyspaceSize => (ulong)Value;
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);

    public override bool IsWorseThan(IEarlyExit other) => (other is LengthLessThanEarlyExit otherExit && Value < otherExit.Value) || (other is LengthNotEqualEarlyExit exact && Value == exact.Value);
}