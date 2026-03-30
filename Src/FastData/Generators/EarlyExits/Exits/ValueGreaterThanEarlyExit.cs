using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey > Value;
public sealed record ValueGreaterThanEarlyExit<T>(T Value) : ValueComparisonEarlyExitBase<T>(Value)
{
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);

    public override bool IsWorseThan(IEarlyExit other) => other is ValueGreaterThanEarlyExit<T> otherExit && Comparer<T>.Default.Compare(Value, otherExit.Value) > 0;
}