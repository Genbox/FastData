using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract record ValueComparisonEarlyExitBase<T>(T Value) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key) => Compare(key, Constant(Value, key.Type));

    protected abstract BinaryExpression Compare(Expression left, Expression right);

    public abstract bool IsWorseThan(IEarlyExit other);
}