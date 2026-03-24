using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Genbox.FastData.Generators.Expressions.Optimizer.Helpers;

internal static class ReductionHelpers
{
    internal static bool TryGetValueBool(Expression expr, out bool value)
    {
        if (expr.NodeType == ExpressionType.Constant && expr is ConstantExpression ce && ce.Type == typeof(bool))
        {
            if (ce.Value is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            value = false;
            return false;
        }

        if (expr.NodeType == ExpressionType.MemberAccess
            && expr is MemberExpression me
            && me.Expression is ConstantExpression ce2
            && ce2.Type == typeof(bool))
        {
            object? ceVal = ce2.Value;
            object? myVal = ceVal;

            if (me.Member is FieldInfo fieldInfo)
                myVal = fieldInfo.GetValue(ceVal);
            else if (me.Member is PropertyInfo propInfo)
                myVal = propInfo.GetValue(ceVal, null);

            if (myVal is bool b)
            {
                value = b;
                return true;
            }
        }

        value = false;
        return false;
    }

    internal static bool IsIntegralType(Type type) => type == typeof(sbyte)
                                                      || type == typeof(byte)
                                                      || type == typeof(short)
                                                      || type == typeof(ushort)
                                                      || type == typeof(int)
                                                      || type == typeof(uint)
                                                      || type == typeof(long)
                                                      || type == typeof(ulong);

    internal static bool IsSignedIntegralType(Type type) => type == typeof(sbyte)
                                                            || type == typeof(short)
                                                            || type == typeof(int)
                                                            || type == typeof(long);

    internal static bool IsFloatingPointType(Type type)
    {
        Type realType = Nullable.GetUnderlyingType(type) ?? type;
        return realType == typeof(float) || realType == typeof(double);
    }

    internal static bool IsZero(ConstantExpression constant)
    {
        object? value = constant.Value;
        if (value == null)
            return false;

        return value switch
        {
            decimal m => m == 0m,
            int i => i == 0,
            uint ui => ui == 0,
            short s => s == 0,
            ushort us => us == 0,
            long l => l == 0L,
            ulong ul => ul == 0UL,
            byte b => b == 0,
            sbyte sb => sb == 0,
            _ => false
        };
    }

    internal static bool IsOne(ConstantExpression constant)
    {
        object? value = constant.Value;
        if (value == null)
            return false;

        return value switch
        {
            decimal m => m == 1m,
            int i => i == 1,
            uint ui => ui == 1,
            short s => s == 1,
            ushort us => us == 1,
            long l => l == 1L,
            ulong ul => ul == 1UL,
            byte b => b == 1,
            sbyte sb => sb == 1,
            _ => false
        };
    }

    internal static bool IsMinusOne(ConstantExpression constant)
    {
        object? value = constant.Value;
        if (value == null)
            return false;

        return value switch
        {
            decimal m => m == -1m,
            sbyte v => v == -1,
            short v => v == -1,
            int v => v == -1,
            long v => v == -1L,
            _ => false
        };
    }

    internal static bool IsAllOnes(ConstantExpression constant)
    {
        object? value = constant.Value;
        if (value == null)
            return false;

        return value switch
        {
            sbyte v => v == -1,
            short v => v == -1,
            int v => v == -1,
            long v => v == -1L,
            byte v => v == byte.MaxValue,
            ushort v => v == ushort.MaxValue,
            uint v => v == uint.MaxValue,
            ulong v => v == ulong.MaxValue,
            _ => false
        };
    }

    internal static bool IsMinValue(ConstantExpression constant)
    {
        object? value = constant.Value;
        if (value == null)
            return false;

        return value switch
        {
            decimal v => v == decimal.MinValue,
            sbyte v => v == sbyte.MinValue,
            byte v => v == byte.MinValue,
            short v => v == short.MinValue,
            ushort v => v == ushort.MinValue,
            int v => v == int.MinValue,
            uint v => v == uint.MinValue,
            long v => v == long.MinValue,
            ulong v => v == ulong.MinValue,
            _ => false
        };
    }

    internal static bool IsMaxValue(ConstantExpression constant)
    {
        object? value = constant.Value;
        if (value == null)
            return false;

        return value switch
        {
            decimal v => v == decimal.MaxValue,
            sbyte v => v == sbyte.MaxValue,
            byte v => v == byte.MaxValue,
            short v => v == short.MaxValue,
            ushort v => v == ushort.MaxValue,
            int v => v == int.MaxValue,
            uint v => v == uint.MaxValue,
            long v => v == long.MaxValue,
            ulong v => v == ulong.MaxValue,
            _ => false
        };
    }

    internal static ConstantExpression CreateZeroConstant(Type type)
    {
        if (type == typeof(sbyte))
            return Constant((sbyte)0, type);
        if (type == typeof(byte))
            return Constant((byte)0, type);
        if (type == typeof(short))
            return Constant((short)0, type);
        if (type == typeof(ushort))
            return Constant((ushort)0, type);
        if (type == typeof(int))
            return Constant(0, type);
        if (type == typeof(uint))
            return Constant(0u, type);
        if (type == typeof(long))
            return Constant(0L, type);
        if (type == typeof(ulong))
            return Constant(0uL, type);
        if (type == typeof(float))
            return Constant(0f, type);
        if (type == typeof(double))
            return Constant(0.0, type);
        if (type == typeof(decimal))
            return Constant(0m, type);

        throw new InvalidOperationException("Unsupported integral type for zero constant: " + type);
    }

    internal static ConstantExpression CreateAllOnesConstant(Type type)
    {
        if (type == typeof(sbyte))
            return Constant((sbyte)-1, type);
        if (type == typeof(short))
            return Constant((short)-1, type);
        if (type == typeof(int))
            return Constant(-1, type);
        if (type == typeof(long))
            return Constant(-1L, type);
        if (type == typeof(float))
            return Constant(-1f, type);
        if (type == typeof(double))
            return Constant(-1d, type);
        if (type == typeof(decimal))
            return Constant(-1m, type);
        if (type == typeof(byte))
            return Constant(byte.MaxValue, type);
        if (type == typeof(ushort))
            return Constant(ushort.MaxValue, type);
        if (type == typeof(uint))
            return Constant(uint.MaxValue, type);
        if (type == typeof(ulong))
            return Constant(ulong.MaxValue, type);

        throw new InvalidOperationException("Unsupported integral type for all-ones constant: " + type);
    }

    internal static bool TryGetComparisonExpression(Expression expression, out ExpressionType op, [NotNullWhen(true)]out Expression? left, [NotNullWhen(true)]out Expression? right)
    {
        op = ExpressionType.Default;
        left = null;
        right = null;

        if (expression is not BinaryExpression be)
            return false;

        switch (expression.NodeType)
        {
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                op = expression.NodeType;
                left = be.Left;
                right = be.Right;
                return true;
            default:
                return false;
        }
    }

    internal static bool TryGetIfThenElse(Expression expression, [NotNullWhen(true)]out Expression? test, [NotNullWhen(true)]out Expression? ifTrue, [NotNullWhen(true)]out Expression? ifFalse)
    {
        if (expression.NodeType == ExpressionType.Conditional && expression is ConditionalExpression ce)
        {
            test = ce.Test;
            ifTrue = ce.IfTrue;
            ifFalse = ce.IfFalse;
            return true;
        }

        test = null;
        ifTrue = null;
        ifFalse = null;
        return false;
    }

    internal static bool TryGetNot(Expression expression, [NotNullWhen(true)]out Expression? operand)
    {
        if (expression.NodeType == ExpressionType.Not && expression is UnaryExpression ue)
        {
            operand = ue.Operand;
            return true;
        }

        operand = null;
        return false;
    }

    internal static bool TryGetOr(Expression expression, [NotNullWhen(true)]out Expression? left, [NotNullWhen(true)]out Expression? right)
    {
        if (expression is BinaryExpression be && expression.NodeType == ExpressionType.OrElse)
        {
            left = be.Left;
            right = be.Right;
            return true;
        }

        if (TryGetIfThenElse(expression, out Expression? test, out Expression? ifTrue, out Expression? ifFalse) && TryGetValueBool(ifTrue, out bool value) && value)
        {
            left = test;
            right = ifFalse;
            return true;
        }

        left = null;
        right = null;
        return false;
    }

    internal static bool TryGetAnd(Expression expression, [NotNullWhen(true)]out Expression? left, [NotNullWhen(true)]out Expression? right)
    {
        if (expression is BinaryExpression be && expression.NodeType == ExpressionType.AndAlso)
        {
            left = be.Left;
            right = be.Right;
            return true;
        }

        if (TryGetIfThenElse(expression, out Expression? test, out Expression? ifTrue, out Expression? ifFalse) && TryGetValueBool(ifFalse, out bool value) && !value)
        {
            left = test;
            right = ifTrue;
            return true;
        }

        left = null;
        right = null;
        return false;
    }

    internal static ConstantExpression CreateBool(bool value) => Constant(value, typeof(bool));
}