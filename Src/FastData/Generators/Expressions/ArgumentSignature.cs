using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Expressions;

internal static class ArgumentSignature
{
    internal static bool Equals(Expression left, Expression right)
    {
        ArgumentKind kind = GetKind(left);
        if (kind != GetKind(right) || left.Type != right.Type)
            return false;

        return kind switch
        {
            ArgumentKind.Constant => object.Equals(((ConstantExpression)left).Value, ((ConstantExpression)right).Value),
            ArgumentKind.Parameter => string.Equals(((ParameterExpression)left).Name, ((ParameterExpression)right).Name, StringComparison.Ordinal),
            ArgumentKind.Other => string.Equals(left.ToString(), right.ToString(), StringComparison.Ordinal),
            _ => false
        };
    }

    internal static void AddHashCode(ref HashCode hash, Expression expression)
    {
        ArgumentKind kind = GetKind(expression);
        hash.Add(kind);
        hash.Add(expression.Type);

        switch (kind)
        {
            case ArgumentKind.Constant:
                hash.Add(((ConstantExpression)expression).Value);
                break;
            case ArgumentKind.Parameter:
                hash.Add(((ParameterExpression)expression).Name, StringComparer.Ordinal);
                break;
            case ArgumentKind.Other:
                hash.Add(expression.ToString(), StringComparer.Ordinal);
                break;
        }
    }

    private static ArgumentKind GetKind(Expression expression) => expression switch
    {
        ConstantExpression => ArgumentKind.Constant,
        ParameterExpression => ArgumentKind.Parameter,
        _ => ArgumentKind.Other
    };
}