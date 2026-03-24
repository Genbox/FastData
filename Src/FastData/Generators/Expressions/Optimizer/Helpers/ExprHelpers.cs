using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Genbox.FastData.Generators.Expressions.Optimizer.Helpers;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class ExprHelpers
{
    internal static bool PropertyMatch(Expression? left, Expression? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        if (left.NodeType != right.NodeType)
            return false;

        if (left.NodeType == ExpressionType.MemberAccess && left is MemberExpression pe1 && right is MemberExpression pe2)
        {
            if (!Equals(pe1.Member, pe2.Member) || !Equals(pe1.Expression, pe2.Expression))
                return false;

            return string.Equals(pe1.ToString(), pe2.ToString(), StringComparison.Ordinal);
        }

        if (left.NodeType == ExpressionType.Constant && left is ConstantExpression lConst && right is ConstantExpression rConst)
            return Equals(lConst.Value, rConst.Value);

        if (left.NodeType == ExpressionType.Parameter && left is ParameterExpression lPar && right is ParameterExpression rPar)
            return ReferenceEquals(lPar, rPar);

        return false;
    }

    private static bool TryGetCorrectType(object? value, Type parentType, [NotNullWhen(true)]out IComparable? comparable)
    {
        if (value is IComparable candidate && value.GetType() == parentType)
        {
            comparable = candidate;
            return true;
        }

        comparable = null;
        return false;
    }

    internal static bool TryGetConstantBasicType(Expression parentExpr, Expression expr, [NotNullWhen(true)]out IComparable? result)
    {
        result = null;

        if (expr.NodeType != ExpressionType.Constant || expr is not ConstantExpression ce)
            return false;

        if (!parentExpr.Type.IsPrimitive && parentExpr.Type != typeof(decimal))
            return false;

        if (ce.Value == null)
            return false;

        if (TryGetCorrectType(ce.Value, parentExpr.Type, out result))
            return true;

        object? myVal;
        if (parentExpr.NodeType == ExpressionType.MemberAccess && parentExpr is MemberExpression mainNode)
        {
            myVal = mainNode.Member switch
            {
                FieldInfo fieldInfo => fieldInfo.GetValue(ce.Value),
                PropertyInfo propInfo => propInfo.GetValue(ce.Value, null),
                _ => ce.Value
            };
        }
        else
            myVal = ce.Value;

        if (myVal == null)
            return false;

        return TryGetCorrectType(myVal, parentExpr.Type, out result);
    }

    internal static bool TryGetConstantComparable(Expression expression, [NotNullWhen(true)]out IComparable? result)
    {
        if (expression.NodeType == ExpressionType.Constant && expression is ConstantExpression ce && ce.Value is IComparable c)
        {
            result = c;
            return true;
        }

        if (expression.NodeType == ExpressionType.Convert && expression is UnaryExpression ue)
            return TryGetConstantBasicType(ue, ue.Operand, out result);

        result = null;
        return false;
    }

    internal static bool TryGetFloatingComparison(ExpressionType nodeType, object left, object right, out bool result)
    {
        if (left is double ld && right is double rd)
        {
            result = nodeType switch
            {
                ExpressionType.Equal => ld == rd,
                ExpressionType.NotEqual => ld != rd,
                ExpressionType.LessThan => ld < rd,
                ExpressionType.LessThanOrEqual => ld <= rd,
                ExpressionType.GreaterThan => ld > rd,
                ExpressionType.GreaterThanOrEqual => ld >= rd,
                _ => false
            };
            return true;
        }

        if (left is float lf && right is float rf)
        {
            result = nodeType switch
            {
                ExpressionType.Equal => lf == rf,
                ExpressionType.NotEqual => lf != rf,
                ExpressionType.LessThan => lf < rf,
                ExpressionType.LessThanOrEqual => lf <= rf,
                ExpressionType.GreaterThan => lf > rf,
                ExpressionType.GreaterThanOrEqual => lf >= rf,
                _ => false
            };
            return true;
        }

        result = false;
        return false;
    }
}