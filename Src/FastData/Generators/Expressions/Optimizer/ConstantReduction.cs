using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using static Genbox.FastData.Generators.Expressions.Optimizer.Helpers.ExprHelpers;
using static Genbox.FastData.Generators.Expressions.Optimizer.Helpers.ReductionHelpers;

namespace Genbox.FastData.Generators.Expressions.Optimizer;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class ConstantReduction
{
    internal static Expression ReduceStaticConditional(Expression expr)
    {
        // true ? x : y -> x
        // false ? x : y -> y

        if (expr.NodeType != ExpressionType.Conditional || expr is not ConditionalExpression ce)
            return expr;

        if (!TryGetValueBool(ce.Test, out bool testValue))
            return expr;

        return testValue ? ce.IfTrue : ce.IfFalse;
    }

    internal static Expression FoldConstants(Expression expr)
    {
        // SomeType.ConstField -> 123

        if (expr.NodeType != ExpressionType.MemberAccess || expr is not MemberExpression me)
            return expr;

        if (me.Expression != null)
            return TryGetConstantBasicType(me, me.Expression, out IComparable? constant) ? Constant(constant, me.Type) : expr;

        // Support static literal fields (no instance expression).
        if (me.Member is FieldInfo fieldInfo
            && fieldInfo.IsLiteral
            && (me.Type.IsPrimitive || me.Type == typeof(decimal)))
        {
            object? rawValue = fieldInfo.GetRawConstantValue();
            if (rawValue is IComparable literal && literal.GetType() == me.Type)
                return Constant(literal, me.Type);
        }

        return expr;
    }

    internal static Expression EvaluateConstantMath(Expression expr)
    {
        // 2 + 3 -> 5
        // (x + 1) + 2 -> x + 3
        // (x - 1) - 2 -> x - 3
        // (x * 2) * 3 -> x * 6
        // x + 0 -> x
        // x * 1 -> x
        // x / 1 -> x

        if (expr is not BinaryExpression be)
            return expr;

        ExpressionType nodeType = expr.NodeType;
        if ((nodeType != ExpressionType.Add
             && nodeType != ExpressionType.AddChecked
             && nodeType != ExpressionType.Subtract
             && nodeType != ExpressionType.SubtractChecked
             && nodeType != ExpressionType.Multiply
             && nodeType != ExpressionType.MultiplyChecked
             && nodeType != ExpressionType.Divide
             && nodeType != ExpressionType.Modulo)
            || be.Left.Type != be.Right.Type)
            return expr;

        if (be.Left.NodeType == ExpressionType.Constant
            && be.Right.NodeType == ExpressionType.Constant
            && be.Left is ConstantExpression ceLeft
            && be.Right is ConstantExpression ceRight)
        {
            // Don't fold when it is a divide by zero. Let them happen at runtime.
            if (nodeType is ExpressionType.Divide or ExpressionType.Modulo && IsZero(ceRight))
                return expr;

            bool chk = nodeType is ExpressionType.AddChecked or ExpressionType.SubtractChecked or ExpressionType.MultiplyChecked;

            try
            {
                return nodeType switch
                {
                    ExpressionType.Add or ExpressionType.AddChecked => (ceLeft.Value, ceRight.Value) switch
                    {
                        (string l, string r) when ceLeft.Type == typeof(string) => Constant(l + r, ceLeft.Type),
                        (decimal l, decimal r) when ceLeft.Type == typeof(decimal) => Constant(l + r, ceLeft.Type),
                        (float l, float r) when ceLeft.Type == typeof(float) => Constant(l + r, ceLeft.Type),
                        (double l, double r) when ceLeft.Type == typeof(double) => Constant(l + r, ceLeft.Type),
                        (int l, int r) when ceLeft.Type == typeof(int) => Constant(chk ? checked(l + r) : l + r, ceLeft.Type),
                        (long l, long r) when ceLeft.Type == typeof(long) => Constant(chk ? checked(l + r) : l + r, ceLeft.Type),
                        (uint l, uint r) when ceLeft.Type == typeof(uint) => Constant(chk ? checked(l + r) : l + r, ceLeft.Type),
                        (ulong l, ulong r) when ceLeft.Type == typeof(ulong) => Constant(chk ? checked(l + r) : l + r, ceLeft.Type),
                        (short l, short r) when ceLeft.Type == typeof(short) => Constant(chk ? checked((short)(l + r)) : (short)(l + r), ceLeft.Type),
                        (ushort l, ushort r) when ceLeft.Type == typeof(ushort) => Constant(chk ? checked((ushort)(l + r)) : (ushort)(l + r), ceLeft.Type),
                        (sbyte l, sbyte r) when ceLeft.Type == typeof(sbyte) => Constant(chk ? checked((sbyte)(l + r)) : (sbyte)(l + r), ceLeft.Type),
                        (byte l, byte r) when ceLeft.Type == typeof(byte) => Constant(chk ? checked((byte)(l + r)) : (byte)(l + r), ceLeft.Type),
                        _ => expr
                    },
                    ExpressionType.Subtract or ExpressionType.SubtractChecked => (ceLeft.Value, ceRight.Value) switch
                    {
                        (decimal l, decimal r) when ceLeft.Type == typeof(decimal) => Constant(l - r, ceLeft.Type),
                        (float l, float r) when ceLeft.Type == typeof(float) => Constant(l - r, ceLeft.Type),
                        (double l, double r) when ceLeft.Type == typeof(double) => Constant(l - r, ceLeft.Type),
                        (int l, int r) when ceLeft.Type == typeof(int) => Constant(chk ? checked(l - r) : l - r, ceLeft.Type),
                        (long l, long r) when ceLeft.Type == typeof(long) => Constant(chk ? checked(l - r) : l - r, ceLeft.Type),
                        (uint l, uint r) when ceLeft.Type == typeof(uint) => Constant(chk ? checked(l - r) : l - r, ceLeft.Type),
                        (ulong l, ulong r) when ceLeft.Type == typeof(ulong) => Constant(chk ? checked(l - r) : l - r, ceLeft.Type),
                        (short l, short r) when ceLeft.Type == typeof(short) => Constant(chk ? checked((short)(l - r)) : (short)(l - r), ceLeft.Type),
                        (ushort l, ushort r) when ceLeft.Type == typeof(ushort) => Constant(chk ? checked((ushort)(l - r)) : (ushort)(l - r), ceLeft.Type),
                        (sbyte l, sbyte r) when ceLeft.Type == typeof(sbyte) => Constant(chk ? checked((sbyte)(l - r)) : (sbyte)(l - r), ceLeft.Type),
                        (byte l, byte r) when ceLeft.Type == typeof(byte) => Constant(chk ? checked((byte)(l - r)) : (byte)(l - r), ceLeft.Type),
                        _ => expr
                    },
                    ExpressionType.Multiply or ExpressionType.MultiplyChecked => (ceLeft.Value, ceRight.Value) switch
                    {
                        (decimal l, decimal r) when ceLeft.Type == typeof(decimal) => Constant(l * r, ceLeft.Type),
                        (float l, float r) when ceLeft.Type == typeof(float) => Constant(l * r, ceLeft.Type),
                        (double l, double r) when ceLeft.Type == typeof(double) => Constant(l * r, ceLeft.Type),
                        (int l, int r) when ceLeft.Type == typeof(int) => Constant(chk ? checked(l * r) : l * r, ceLeft.Type),
                        (long l, long r) when ceLeft.Type == typeof(long) => Constant(chk ? checked(l * r) : l * r, ceLeft.Type),
                        (uint l, uint r) when ceLeft.Type == typeof(uint) => Constant(chk ? checked(l * r) : l * r, ceLeft.Type),
                        (ulong l, ulong r) when ceLeft.Type == typeof(ulong) => Constant(chk ? checked(l * r) : l * r, ceLeft.Type),
                        (short l, short r) when ceLeft.Type == typeof(short) => Constant(chk ? checked((short)(l * r)) : (short)(l * r), ceLeft.Type),
                        (ushort l, ushort r) when ceLeft.Type == typeof(ushort) => Constant(chk ? checked((ushort)(l * r)) : (ushort)(l * r), ceLeft.Type),
                        (sbyte l, sbyte r) when ceLeft.Type == typeof(sbyte) => Constant(chk ? checked((sbyte)(l * r)) : (sbyte)(l * r), ceLeft.Type),
                        (byte l, byte r) when ceLeft.Type == typeof(byte) => Constant(chk ? checked((byte)(l * r)) : (byte)(l * r), ceLeft.Type),
                        _ => expr
                    },
                    ExpressionType.Divide => (ceLeft.Value, ceRight.Value) switch
                    {
                        (decimal l, decimal r) when ceLeft.Type == typeof(decimal) => Constant(l / r, ceLeft.Type),
                        (float l, float r) when ceLeft.Type == typeof(float) => Constant(l / r, ceLeft.Type),
                        (double l, double r) when ceLeft.Type == typeof(double) => Constant(l / r, ceLeft.Type),
                        (int l, int r) when ceLeft.Type == typeof(int) => Constant(l / r, ceLeft.Type),
                        (long l, long r) when ceLeft.Type == typeof(long) => Constant(l / r, ceLeft.Type),
                        (uint l, uint r) when ceLeft.Type == typeof(uint) => Constant(l / r, ceLeft.Type),
                        (ulong l, ulong r) when ceLeft.Type == typeof(ulong) => Constant(l / r, ceLeft.Type),
                        (short l, short r) when ceLeft.Type == typeof(short) => Constant((short)(l / r), ceLeft.Type),
                        (ushort l, ushort r) when ceLeft.Type == typeof(ushort) => Constant((ushort)(l / r), ceLeft.Type),
                        (sbyte l, sbyte r) when ceLeft.Type == typeof(sbyte) => Constant((sbyte)(l / r), ceLeft.Type),
                        (byte l, byte r) when ceLeft.Type == typeof(byte) => Constant((byte)(l / r), ceLeft.Type),
                        _ => expr
                    },
                    ExpressionType.Modulo => (ceLeft.Value, ceRight.Value) switch
                    {
                        (decimal l, decimal r) when ceLeft.Type == typeof(decimal) => Constant(l % r, ceLeft.Type),
                        (float l, float r) when ceLeft.Type == typeof(float) => Constant(l % r, ceLeft.Type),
                        (double l, double r) when ceLeft.Type == typeof(double) => Constant(l % r, ceLeft.Type),
                        (int l, int r) when ceLeft.Type == typeof(int) => Constant(l % r, ceLeft.Type),
                        (long l, long r) when ceLeft.Type == typeof(long) => Constant(l % r, ceLeft.Type),
                        (uint l, uint r) when ceLeft.Type == typeof(uint) => Constant(l % r, ceLeft.Type),
                        (ulong l, ulong r) when ceLeft.Type == typeof(ulong) => Constant(l % r, ceLeft.Type),
                        (short l, short r) when ceLeft.Type == typeof(short) => Constant((short)(l % r), ceLeft.Type),
                        (ushort l, ushort r) when ceLeft.Type == typeof(ushort) => Constant((ushort)(l % r), ceLeft.Type),
                        (sbyte l, sbyte r) when ceLeft.Type == typeof(sbyte) => Constant((sbyte)(l % r), ceLeft.Type),
                        (byte l, byte r) when ceLeft.Type == typeof(byte) => Constant((byte)(l % r), ceLeft.Type),
                        _ => expr
                    },
                    _ => expr
                };
            }
            catch (DivideByZeroException)
            {
                return expr;
            }
            catch (OverflowException)
            {
                return expr;
            }
        }

        if (nodeType == ExpressionType.Add && be.Right.NodeType == ExpressionType.Constant
                                           && be.Left is BinaryExpression beLeft
                                           && beLeft.NodeType == ExpressionType.Add
                                           && be.Right is ConstantExpression right)
        {
            if (beLeft.Method == null && be.Method == null)
            {
                if (beLeft.Left.NodeType == ExpressionType.Constant && beLeft.Left is ConstantExpression beLeftLeft && beLeftLeft.Type == right.Type)
                    return AddConstants(beLeft.Right, beLeftLeft, right) ?? expr;

                if (beLeft.Right.NodeType == ExpressionType.Constant && beLeft.Right is ConstantExpression beLeftRight && beLeftRight.Type == right.Type)
                    return AddConstants(beLeft.Left, beLeftRight, right) ?? expr;
            }
        }

        if (nodeType == ExpressionType.Subtract && be.Right.NodeType == ExpressionType.Constant
                                                && be.Left is BinaryExpression beLeft2
                                                && beLeft2.NodeType == ExpressionType.Subtract
                                                && be.Right is ConstantExpression rightSub
                                                && beLeft2.Method == null
                                                && be.Method == null)
        {
            if (beLeft2.Right.NodeType == ExpressionType.Constant && beLeft2.Right is ConstantExpression beLeftRight && beLeftRight.Type == rightSub.Type)
                return SubtractConstants(beLeft2.Left, beLeftRight, rightSub) ?? expr;
        }

        if (nodeType == ExpressionType.Multiply && be.Right.NodeType == ExpressionType.Constant
                                                && be.Left is BinaryExpression beLeft3
                                                && beLeft3.NodeType == ExpressionType.Multiply
                                                && be.Right is ConstantExpression rightMul
                                                && beLeft3.Method == null
                                                && be.Method == null)
        {
            if (beLeft3.Left.NodeType == ExpressionType.Constant && beLeft3.Left is ConstantExpression beLeftLeft && beLeftLeft.Type == rightMul.Type)
                return MultiplyConstants(beLeft3.Right, beLeftLeft, rightMul) ?? expr;

            if (beLeft3.Right.NodeType == ExpressionType.Constant && beLeft3.Right is ConstantExpression beLeftRight && beLeftRight.Type == rightMul.Type)
                return MultiplyConstants(beLeft3.Left, beLeftRight, rightMul) ?? expr;
        }

        if (be.Right.NodeType == ExpressionType.Constant && be.Right is ConstantExpression beRight && nodeType is ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked)
            return RemoveAddIdentity(be.Left, beRight) ?? expr;

        if (be.Left.NodeType == ExpressionType.Constant && be.Left is ConstantExpression beLeft4 && nodeType is ExpressionType.Add or ExpressionType.AddChecked)
            return RemoveAddIdentity(be.Right, beLeft4) ?? expr;

        if (nodeType is ExpressionType.Multiply or ExpressionType.MultiplyChecked && be.Right.NodeType == ExpressionType.Constant && be.Right is ConstantExpression beRight2)
            return RemoveMultiplicativeIdentity(be.Left, beRight2) ?? expr;

        if (nodeType is ExpressionType.Multiply or ExpressionType.MultiplyChecked && be.Left.NodeType == ExpressionType.Constant && be.Left is ConstantExpression beLeft5)
            return RemoveMultiplicativeIdentity(be.Right, beLeft5) ?? expr;

        if (nodeType == ExpressionType.Divide && be.Right.NodeType == ExpressionType.Constant && be.Right is ConstantExpression beRight3)
            return RemoveDivisionIdentity(be.Left, beRight3) ?? expr;

        return expr;
    }

    internal static Expression SimplifyConditionals(Expression expr)
    {
        // cond ? true : false -> cond
        // cond ? false : true -> !cond
        // cond ? x : x -> x
        if (expr.NodeType == ExpressionType.Conditional && expr is ConditionalExpression ce)
        {
            if (TryGetValueBool(ce.IfTrue, out bool tValue) && TryGetValueBool(ce.IfFalse, out bool fValue))
            {
                if (tValue && !fValue)
                    return ce.Test;
                if (!tValue && fValue)
                    return Not(ce.Test);
            }

            if (PropertyMatch(ce.IfTrue, ce.IfFalse))
                return ce.IfTrue;
        }

        return expr;
    }

    internal static Expression SimplifyArithmeticIdentities(Expression expr)
    {
        // x - 0 -> x
        // x - x -> 0
        // x % 1 -> 0

        if (expr.NodeType == ExpressionType.Subtract && expr is BinaryExpression beSub)
        {
            if (beSub.Right.NodeType == ExpressionType.Constant && beSub.Right is ConstantExpression ce)
            {
                Expression? reduced = RemoveAddIdentity(beSub.Left, ce);
                if (reduced != null)
                    return reduced;
            }

            if (PropertyMatch(beSub.Left, beSub.Right))
            {
                Type leftType = beSub.Left.Type;
                return IsIntegralType(leftType) ? CreateZeroConstant(leftType) : expr;
            }
        }

        if (expr.NodeType == ExpressionType.Modulo && expr is BinaryExpression beMod)
        {
            if (beMod.Right.NodeType == ExpressionType.Constant && beMod.Right is ConstantExpression rConst)
            {
                if (IsIntegralType(rConst.Type) && IsOne(rConst))
                    return CreateZeroConstant(beMod.Type);
            }
        }

        return expr;
    }

    internal static Expression SimplifyArithmeticReductions(Expression expr)
    {
        // 0 - x -> -x
        // (-1) - x -> ~x
        // x * -1 -> -x
        if (expr is not BinaryExpression be)
            return expr;

        Type type = be.Type;
        if (!IsIntegralType(type))
            return expr;

        if (be.Left is ConstantExpression lConst)
        {
            if (expr.NodeType is ExpressionType.Subtract or ExpressionType.SubtractChecked)
            {
                if (IsZero(lConst) && IsSignedIntegralType(type))
                    return expr.NodeType == ExpressionType.SubtractChecked ? NegateChecked(be.Right) : Negate(be.Right);

                if (IsAllOnes(lConst))
                    return Not(be.Right);
            }

            if (expr.NodeType == ExpressionType.Multiply && IsMinusOne(lConst) && IsSignedIntegralType(type))
                return Negate(be.Right);
        }

        if (expr.NodeType == ExpressionType.Multiply && be.Right is ConstantExpression rConst && IsMinusOne(rConst) && IsSignedIntegralType(type))
            return Negate(be.Left);

        return expr;
    }

    internal static Expression SimplifyBitwise(Expression expr)
    {
        // (x ^ y) ^ x -> y
        // x ^ x -> 0
        // x & 0 -> 0
        // x & ~0 -> x
        // x | 0 -> x
        // x | ~0 -> ~0
        // x ^ 0 -> x
        // x ^ ~0 -> ~x
        if (expr is not BinaryExpression be)
            return expr;

        if (expr.NodeType is not (ExpressionType.And or ExpressionType.Or or ExpressionType.ExclusiveOr))
            return expr;

        Type type = be.Type;
        if (!IsIntegralType(type))
            return expr;

        if (expr.NodeType == ExpressionType.ExclusiveOr)
        {
            if (be.Left is BinaryExpression leftXor && leftXor.NodeType == ExpressionType.ExclusiveOr)
            {
                if (PropertyMatch(leftXor.Left, be.Right))
                    return leftXor.Right;
                if (PropertyMatch(leftXor.Right, be.Right))
                    return leftXor.Left;
            }

            if (be.Right is BinaryExpression rightXor && rightXor.NodeType == ExpressionType.ExclusiveOr)
            {
                if (PropertyMatch(rightXor.Left, be.Left))
                    return rightXor.Right;
                if (PropertyMatch(rightXor.Right, be.Left))
                    return rightXor.Left;
            }
        }

        if (PropertyMatch(be.Left, be.Right))
            return expr.NodeType == ExpressionType.ExclusiveOr ? CreateZeroConstant(type) : be.Left;

        if (be.Left is ConstantExpression leftConst)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.And when IsAllOnes(leftConst):
                case ExpressionType.Or when IsZero(leftConst):
                case ExpressionType.ExclusiveOr when IsZero(leftConst):
                    return be.Right;
                case ExpressionType.And when IsZero(leftConst):
                    return CreateZeroConstant(type);
                case ExpressionType.Or when IsAllOnes(leftConst):
                    return CreateAllOnesConstant(type);
                case ExpressionType.ExclusiveOr when IsAllOnes(leftConst):
                    return Not(be.Right);
            }
        }

        if (be.Right is ConstantExpression rightConst)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.And when IsAllOnes(rightConst):
                case ExpressionType.Or when IsZero(rightConst):
                case ExpressionType.ExclusiveOr when IsZero(rightConst):
                    return be.Left;
                case ExpressionType.And when IsZero(rightConst):
                    return CreateZeroConstant(type);
                case ExpressionType.Or when IsAllOnes(rightConst):
                    return CreateAllOnesConstant(type);
                case ExpressionType.ExclusiveOr when IsAllOnes(rightConst):
                    return Not(be.Left);
            }
        }

        return expr;
    }

    internal static Expression SimplifyShift(Expression expr)
    {
        // x << 0 -> x
        // 0 << n -> 0
        // x >> 0 -> x
        // 0 >> n -> 0
        if (expr is not BinaryExpression be)
            return expr;

        if ((expr.NodeType != ExpressionType.LeftShift && expr.NodeType != ExpressionType.RightShift) || !IsIntegralType(be.Left.Type))
            return expr;

        if (be.Right is ConstantExpression rConst && rConst.Type == typeof(int) && rConst.Value is int r && r == 0)
            return be.Left;

        if (be.Left is ConstantExpression lConst && IsZero(lConst))
            return CreateZeroConstant(be.Left.Type);

        return expr;
    }

    private static Expression? AddConstants(Expression expr, ConstantExpression lConst, ConstantExpression rConst)
    {
        try
        {
            return (lConst.Value, rConst.Value) switch
            {
                // (x + 1) + 2 -> x + 3
                (float l, float r) => Add(expr, Constant(l + r, lConst.Type)),
                (double l, double r) => Add(expr, Constant(l + r, lConst.Type)),
                (decimal l, decimal r) => Add(expr, Constant(l + r, lConst.Type)),
                (int l, int r) => Add(expr, Constant(unchecked(l + r), lConst.Type)),
                (long l, long r) => Add(expr, Constant(unchecked(l + r), lConst.Type)),
                (uint l, uint r) => Add(expr, Constant(unchecked(l + r), lConst.Type)),
                (ulong l, ulong r) => Add(expr, Constant(unchecked(l + r), lConst.Type)),
                (short l, short r) => Add(expr, Constant(unchecked((short)(l + r)), lConst.Type)),
                (ushort l, ushort r) => Add(expr, Constant(unchecked((ushort)(l + r)), lConst.Type)),
                (sbyte l, sbyte r) => Add(expr, Constant(unchecked((sbyte)(l + r)), lConst.Type)),
                (byte l, byte r) => Add(expr, Constant(unchecked((byte)(l + r)), lConst.Type)),
                _ => null
            };
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static Expression? SubtractConstants(Expression expr, ConstantExpression lConst, ConstantExpression rConst)
    {
        try
        {
            return (lConst.Value, rConst.Value) switch
            {
                // (x - 1) - 2 -> x - 3
                (float l, float r) => Subtract(expr, Constant(l - r, lConst.Type)),
                (double l, double r) => Subtract(expr, Constant(l - r, lConst.Type)),
                (decimal l, decimal r) => Subtract(expr, Constant(l - r, lConst.Type)),
                (int l, int r) => Subtract(expr, Constant(unchecked(l - r), lConst.Type)),
                (long l, long r) => Subtract(expr, Constant(unchecked(l - r), lConst.Type)),
                (uint l, uint r) => Subtract(expr, Constant(unchecked(l - r), lConst.Type)),
                (ulong l, ulong r) => Subtract(expr, Constant(unchecked(l - r), lConst.Type)),
                (short l, short r) => Subtract(expr, Constant(unchecked((short)(l - r)), lConst.Type)),
                (ushort l, ushort r) => Subtract(expr, Constant(unchecked((ushort)(l - r)), lConst.Type)),
                (sbyte l, sbyte r) => Subtract(expr, Constant(unchecked((sbyte)(l - r)), lConst.Type)),
                (byte l, byte r) => Subtract(expr, Constant(unchecked((byte)(l - r)), lConst.Type)),
                _ => null
            };
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static Expression? MultiplyConstants(Expression expr, ConstantExpression lConst, ConstantExpression rConst)
    {
        try
        {
            return (lConst.Value, rConst.Value) switch
            {
                // (x * 2) * 3 -> x * 6
                (float l, float r) => Multiply(expr, Constant(l * r, lConst.Type)),
                (double l, double r) => Multiply(expr, Constant(l * r, lConst.Type)),
                (decimal l, decimal r) => Multiply(expr, Constant(l * r, lConst.Type)),
                (int l, int r) => Multiply(expr, Constant(unchecked(l * r), lConst.Type)),
                (long l, long r) => Multiply(expr, Constant(unchecked(l * r), lConst.Type)),
                (uint l, uint r) => Multiply(expr, Constant(unchecked(l * r), lConst.Type)),
                (ulong l, ulong r) => Multiply(expr, Constant(unchecked(l * r), lConst.Type)),
                (short l, short r) => Multiply(expr, Constant(unchecked((short)(l * r)), lConst.Type)),
                (ushort l, ushort r) => Multiply(expr, Constant(unchecked((ushort)(l * r)), lConst.Type)),
                (sbyte l, sbyte r) => Multiply(expr, Constant(unchecked((sbyte)(l * r)), lConst.Type)),
                (byte l, byte r) => Multiply(expr, Constant(unchecked((byte)(l * r)), lConst.Type)),
                _ => null
            };
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static Expression? RemoveAddIdentity(Expression expr, ConstantExpression constant)
    {
        // x + 0 -> x
        // 0 + x -> x

        if (!IsZero(constant))
            return null;

        return IsFloatingPointType(constant.Type) ? null : expr;
    }

    private static Expression? RemoveMultiplicativeIdentity(Expression expr, ConstantExpression constant)
    {
        // x * 1 -> x
        // x * 0 -> 0

        if (IsOne(constant))
            return expr;

        if (IsZero(constant))
            return IsFloatingPointType(constant.Type) ? null : CreateZeroConstant(constant.Type);

        return null;
    }

    // x / 1 -> x
    private static Expression? RemoveDivisionIdentity(Expression expr, ConstantExpression constant) => IsOne(constant) ? expr : null;
}