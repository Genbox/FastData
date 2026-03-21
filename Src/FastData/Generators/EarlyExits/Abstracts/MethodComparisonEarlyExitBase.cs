using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract class MethodComparisonEarlyExitBase<T>(T value, string method) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(EarlyExitFunctions).GetMethod(method, [typeof(string)])!;
        return Compare(Call(methodInfo, key), Constant(value, typeof(T)));
    }

    protected abstract BinaryExpression Compare(Expression left, Expression right);
}