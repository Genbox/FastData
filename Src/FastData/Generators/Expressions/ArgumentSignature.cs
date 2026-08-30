using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Genbox.FastData.Generators.Expressions;

internal static class ArgumentSignature
{
    internal static bool Equals(Expression? left, Expression? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null)
            return false;

        ArgumentKind kind = GetKind(left);
        if (kind != GetKind(right) || left.Type != right.Type)
            return false;

        return kind switch
        {
            ArgumentKind.Constant => Equals(((ConstantExpression)left).Value, ((ConstantExpression)right).Value),
            ArgumentKind.Parameter => false,
            ArgumentKind.Other => false,
            _ => false
        };
    }

    internal static void AddHashCode(ref HashCode hash, Expression? expression)
    {
        if (expression == null)
        {
            hash.Add(0);
            return;
        }

        ArgumentKind kind = GetKind(expression);
        hash.Add(kind);
        hash.Add(expression.Type);

        switch (kind)
        {
            case ArgumentKind.Constant:
                hash.Add(((ConstantExpression)expression).Value);
                break;
            case ArgumentKind.Parameter:
            case ArgumentKind.Other:
                hash.Add(RuntimeHelpers.GetHashCode(expression));
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