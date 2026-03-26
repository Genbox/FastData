using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract record MethodComparisonEarlyExitBase<T>(T Value, string Method) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(EarlyExitFunctions).GetMethod(Method, [typeof(string)])!;
        return Compare(Call(methodInfo, key), Constant(Value, typeof(T)));
    }

    protected abstract BinaryExpression Compare(Expression left, Expression right);
}