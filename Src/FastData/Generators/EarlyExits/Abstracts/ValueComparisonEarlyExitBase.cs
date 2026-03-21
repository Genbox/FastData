using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract class ValueComparisonEarlyExitBase<T>(T value) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key) => Compare(key, Constant(value, key.Type));

    protected abstract BinaryExpression Compare(Expression left, Expression right);
}