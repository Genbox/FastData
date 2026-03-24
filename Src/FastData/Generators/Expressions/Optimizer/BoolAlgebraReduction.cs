using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using static Genbox.FastData.Generators.Expressions.Optimizer.Helpers.ExprHelpers;
using static Genbox.FastData.Generators.Expressions.Optimizer.Helpers.ReductionHelpers;

namespace Genbox.FastData.Generators.Expressions.Optimizer;

internal static class BoolAlgebraReduction
{
    internal static Expression Commute(Expression expr)
    {
        // cond ? true : x -> cond || x
        // cond ? x : false -> cond && x

        if (expr.NodeType != ExpressionType.OrElse && TryGetOr(expr, out Expression? left, out Expression? right))
            return OrElse(left, right);

        if (expr.NodeType != ExpressionType.AndAlso && TryGetAnd(expr, out Expression? leftAnd, out Expression? rightAnd))
            return AndAlso(leftAnd, rightAnd);

        return expr;
    }

    internal static Expression Gather(Expression expr)
    {
        // (a || b) && (a || c) -> a || (b && c)
        // (a && b) || (a && c) -> a && (b || c)

        if (TryGetAnd(expr, out Expression? left, out Expression? right) && TryGetOr(left, out Expression? leftOrLeft, out Expression? leftOrRight) && TryGetOr(right, out Expression? rightOrLeft, out Expression? rightOrRight))
        {
            if (PropertyMatch(leftOrLeft, rightOrLeft))
                return OrElse(leftOrLeft, AndAlso(leftOrRight, rightOrRight));
            if (PropertyMatch(leftOrLeft, rightOrRight))
                return OrElse(leftOrLeft, AndAlso(leftOrRight, rightOrLeft));
        }

        if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr) && TryGetAnd(leftOr, out Expression? leftAndLeft, out Expression? leftAndRight) && TryGetAnd(rightOr, out Expression? rightAndLeft, out Expression? rightAndRight))
        {
            if (PropertyMatch(leftAndLeft, rightAndLeft))
                return AndAlso(leftAndLeft, OrElse(leftAndRight, rightAndRight));
            if (PropertyMatch(leftAndLeft, rightAndRight))
                return AndAlso(leftAndLeft, OrElse(leftAndRight, rightAndLeft));
        }

        return expr;
    }

    internal static Expression FactorComplement(Expression expr)
    {
        // (a && b) || (!a && b) -> b
        // (!a && b) || (a && b) -> b

        if (TryGetOr(expr, out Expression? left, out Expression? right) && TryGetComplementAndRest(left, right, out Expression? rest))
            return rest;

        return expr;
    }

    internal static Expression Identity(Expression expr)
    {
        // x && true -> x
        // true && x -> x
        // x || false -> x
        // false || x -> x

        if (TryGetAnd(expr, out Expression? left, out Expression? right))
        {
            if (TryGetValueBool(left, out bool leftValue) && leftValue)
                return right;

            if (TryGetValueBool(right, out bool rightValue) && rightValue)
                return left;
        }

        if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr))
        {
            if (TryGetValueBool(leftOr, out bool leftValue) && !leftValue)
                return rightOr;

            if (TryGetValueBool(rightOr, out bool rightValue) && !rightValue)
                return leftOr;
        }

        return expr;
    }

    internal static Expression SimplifyNotBool(Expression expr)
    {
        // !false -> true
        // !true -> false

        if (expr.NodeType != ExpressionType.Not || expr is not UnaryExpression ue)
            return expr;

        return TryGetValueBool(ue.Operand, out bool value) ? CreateBool(!value) : expr;
    }

    internal static Expression SimplifyBoolean(Expression expr)
    {
        // x == true -> x
        // x == false -> !x
        // x != true -> !x
        // x != false -> x
        if (expr is not BinaryExpression be)
            return expr;

        if (expr.NodeType != ExpressionType.Equal
            && expr.NodeType != ExpressionType.NotEqual
            || be.Left.Type != typeof(bool)
            || be.Right.Type != typeof(bool)
            || be.Type != typeof(bool))
            return expr;

        if (TryGetValueBool(be.Left, out bool leftValue))
            return expr.NodeType == ExpressionType.Equal ? leftValue ? be.Right : Not(be.Right) :
                leftValue ? Not(be.Right) : be.Right;

        if (TryGetValueBool(be.Right, out bool rightValue))
            return expr.NodeType == ExpressionType.Equal ? rightValue ? be.Left : Not(be.Left) :
                rightValue ? Not(be.Left) : be.Left;

        return expr;
    }

    internal static Expression Annihilate(Expression expr)
    {
        // x && false -> false
        // false && x -> false
        // x || true -> true
        // true || x -> true

        if (TryGetAnd(expr, out Expression? left, out Expression? right))
        {
            if (TryGetValueBool(left, out bool leftValue) && !leftValue)
                return CreateBool(false);

            if (TryGetValueBool(right, out bool rightValue) && !rightValue)
                return CreateBool(false);
        }

        if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr))
        {
            if (TryGetValueBool(leftOr, out bool leftValue) && leftValue)
                return CreateBool(true);

            if (TryGetValueBool(rightOr, out bool rightValue) && rightValue)
                return CreateBool(true);
        }

        return expr;
    }

    internal static Expression Absorb(Expression expr)
    {
        // x && (x || y) -> x
        // x || (x && y) -> x

        if (TryGetAnd(expr, out Expression? left, out Expression? right))
        {
            if (TryGetOr(right, out Expression? rightOrLeft, out Expression? rightOrRight) && MatchesEither(left, rightOrLeft, rightOrRight))
                return left;

            if (TryGetOr(left, out Expression? leftOrLeft, out Expression? leftOrRight) && MatchesEither(right, leftOrLeft, leftOrRight))
                return right;
        }

        if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr))
        {
            if (TryGetAnd(rightOr, out Expression? rightAndLeft, out Expression? rightAndRight) && MatchesEither(leftOr, rightAndLeft, rightAndRight))
                return leftOr;

            if (TryGetAnd(leftOr, out Expression? leftAndLeft, out Expression? leftAndRight) && MatchesEither(rightOr, leftAndLeft, leftAndRight))
                return rightOr;
        }

        return expr;
    }

    internal static Expression Idempotence(Expression expr)
    {
        // x && x -> x
        // x || x -> x
        // !(x && x) -> !x
        // !(x || x) -> !x

        {
            if (TryGetAnd(expr, out Expression? p, out Expression? right) && PropertyMatch(p, right))
                return p;
        }

        {
            if (TryGetOr(expr, out Expression? p, out Expression? right) && PropertyMatch(p, right))
                return p;
        }

        if (TryGetNot(expr, out Expression? notOperand))
        {
            {
                if (TryGetAnd(notOperand, out Expression? p, out Expression? right) && PropertyMatch(p, right))
                    return Not(p);
            }

            {
                if (TryGetOr(notOperand, out Expression? p, out Expression? right) && PropertyMatch(p, right))
                    return Not(p);
            }
        }

        return expr;
    }

    internal static Expression Complement(Expression expr)
    {
        // x && !x -> false
        // x || !x -> true

        if (TryGetAnd(expr, out Expression? left, out Expression? right) && IsComplementPair(left, right))
            return CreateBool(false);

        if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr) && IsComplementPair(leftOr, rightOr))
            return CreateBool(true);

        return expr;
    }

    internal static Expression CommuteAbsorb(Expression expr)
    {
        // (x || (a || y)) && a -> a
        // a && (b && (a || c)) -> a && b
        // a || (b || (a && c)) -> a || b

        {
            if (TryGetAnd(expr, out Expression? left, out Expression? right))
            {
                {
                    if (TryGetOr(left, out Expression? _, out Expression? leftOrRight) && TryGetOr(leftOrRight, out Expression? candidate, out Expression? _) && PropertyMatch(candidate, right))
                        return candidate;
                }

                {
                    if (TryGetOr(left, out Expression? leftOrLeft, out Expression? _) && TryGetOr(leftOrLeft, out Expression? candidate, out Expression? _) && PropertyMatch(candidate, right))
                        return candidate;
                }

                {
                    if (TryGetOr(left, out Expression? _, out Expression? leftOrRight) && TryGetOr(leftOrRight, out Expression? _, out Expression? candidate) && PropertyMatch(candidate, right))
                        return candidate;
                }

                {
                    if (TryGetOr(left, out Expression? leftOrLeft, out Expression? _) && TryGetOr(leftOrLeft, out Expression? _, out Expression? candidate) && PropertyMatch(candidate, right))
                        return candidate;
                }

                {
                    if (TryGetOr(right, out Expression? rightOrLeft, out Expression? _) && TryGetOr(rightOrLeft, out Expression? candidate, out Expression? _) && PropertyMatch(candidate, left))
                        return candidate;
                }

                {
                    if (TryGetOr(right, out Expression? _, out Expression? rightOrRight) && TryGetOr(rightOrRight, out Expression? candidate, out Expression? _) && PropertyMatch(candidate, left))
                        return candidate;
                }

                {
                    if (TryGetOr(right, out Expression? rightOrLeft, out Expression? _) && TryGetOr(rightOrLeft, out Expression? _, out Expression? candidate) && PropertyMatch(candidate, left))
                        return candidate;
                }

                {
                    if (TryGetOr(right, out Expression? _, out Expression? rightOrRight) && TryGetOr(rightOrRight, out Expression? _, out Expression? candidate) && PropertyMatch(candidate, left))
                        return candidate;
                }

                {
                    if (TryGetAnd(right, out Expression? rightAndLeft, out Expression? rightAndRight) && TryGetOr(rightAndRight, out Expression? candidate, out Expression? _) && PropertyMatch(left, candidate))
                        return AndAlso(left, rightAndLeft);
                }

                {
                    if (TryGetAnd(right, out Expression? rightAndLeft, out Expression? rightAndRight) && TryGetOr(rightAndRight, out Expression? _, out Expression? candidate) && PropertyMatch(left, candidate))
                        return AndAlso(left, rightAndLeft);
                }

                {
                    if (TryGetAnd(right, out Expression? rightAndLeft, out Expression? rightAndRight) && TryGetOr(rightAndLeft, out Expression? candidate, out Expression? _) && PropertyMatch(left, candidate))
                        return AndAlso(left, rightAndRight);
                }

                {
                    if (TryGetAnd(right, out Expression? rightAndLeft, out Expression? rightAndRight) && TryGetOr(rightAndLeft, out Expression? _, out Expression? candidate) && PropertyMatch(left, candidate))
                        return AndAlso(left, rightAndRight);
                }

                {
                    if (TryGetAnd(left, out Expression? leftAndLeft, out Expression? leftAndRight) && TryGetOr(leftAndRight, out Expression? candidate, out Expression? _) && PropertyMatch(right, candidate))
                        return AndAlso(right, leftAndLeft);
                }

                {
                    if (TryGetAnd(left, out Expression? leftAndLeft, out Expression? leftAndRight) && TryGetOr(leftAndRight, out Expression? _, out Expression? candidate) && PropertyMatch(right, candidate))
                        return AndAlso(right, leftAndLeft);
                }

                {
                    if (TryGetAnd(left, out Expression? leftAndLeft, out Expression? leftAndRight) && TryGetOr(leftAndLeft, out Expression? candidate, out Expression? _) && PropertyMatch(right, candidate))
                        return AndAlso(right, leftAndRight);
                }

                {
                    if (TryGetAnd(left, out Expression? leftAndLeft, out Expression? leftAndRight) && TryGetOr(leftAndLeft, out Expression? _, out Expression? candidate) && PropertyMatch(right, candidate))
                        return AndAlso(right, leftAndRight);
                }
            }
        }
        {
            if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr))
            {
                {
                    if (TryGetOr(rightOr, out Expression? rightOrLeft, out Expression? rightOrRight) && TryGetAnd(rightOrRight, out Expression? candidate, out Expression? _) && PropertyMatch(leftOr, candidate))
                        return OrElse(leftOr, rightOrLeft);
                }

                {
                    if (TryGetOr(rightOr, out Expression? rightOrLeft, out Expression? rightOrRight) && TryGetAnd(rightOrRight, out Expression? _, out Expression? candidate) && PropertyMatch(leftOr, candidate))
                        return OrElse(leftOr, rightOrLeft);
                }

                {
                    if (TryGetOr(rightOr, out Expression? rightOrLeft, out Expression? rightOrRight) && TryGetAnd(rightOrLeft, out Expression? candidate, out Expression? _) && PropertyMatch(leftOr, candidate))
                        return OrElse(leftOr, rightOrRight);
                }

                {
                    if (TryGetOr(rightOr, out Expression? rightOrLeft, out Expression? rightOrRight) && TryGetAnd(rightOrLeft, out Expression? _, out Expression? candidate) && PropertyMatch(leftOr, candidate))
                        return OrElse(leftOr, rightOrRight);
                }

                {
                    if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight) && TryGetAnd(leftOrRight, out Expression? candidate, out Expression? _) && PropertyMatch(rightOr, candidate))
                        return OrElse(leftOrLeft, rightOr);
                }

                {
                    if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight) && TryGetAnd(leftOrRight, out Expression? _, out Expression? candidate) && PropertyMatch(rightOr, candidate))
                        return OrElse(leftOrLeft, rightOr);
                }

                {
                    if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight) && TryGetAnd(leftOrLeft, out Expression? candidate, out Expression? _) && PropertyMatch(rightOr, candidate))
                        return OrElse(leftOrRight, rightOr);
                }

                {
                    if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight) && TryGetAnd(leftOrLeft, out Expression? _, out Expression? candidate) && PropertyMatch(rightOr, candidate))
                        return OrElse(leftOrRight, rightOr);
                }

                {
                    if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight) && TryGetAnd(rightOr, out Expression? rightOrLeft, out Expression? _) && PropertyMatch(leftOrLeft, rightOrLeft))
                        return OrElse(leftOrLeft, leftOrRight);
                }

                {
                    if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight) && TryGetAnd(rightOr, out Expression? _, out Expression? rightOrRight) && PropertyMatch(leftOrLeft, rightOrRight))
                        return OrElse(leftOrLeft, leftOrRight);
                }

                {
                    if (TryGetAnd(leftOr, out Expression? leftOrLeft, out Expression? _) && TryGetOr(rightOr, out Expression? rightOrLeft, out Expression? rightOrRight) && PropertyMatch(leftOrLeft, rightOrLeft))
                        return OrElse(rightOrLeft, rightOrRight);
                }

                {
                    if (TryGetAnd(leftOr, out Expression? _, out Expression? leftOrRight) && TryGetOr(rightOr, out Expression? rightOrLeft, out Expression? rightOrRight) && PropertyMatch(leftOrRight, rightOrLeft))
                        return OrElse(rightOrLeft, rightOrRight);
                }
            }
        }

        return expr;
    }

    internal static Expression DistributeComplement(Expression expr)
    {
        // a && (!a || b) -> a && b
        // (!a || b) && a -> a && b
        // a || (!a && b) -> a || b
        // !(a || b) || (a && !b) -> !b
        // (a || x) || (!a || y) -> true

        if (TryGetAnd(expr, out Expression? left, out Expression? right))
        {
            {
                if (TryGetOr(right, out Expression? rightNot, out Expression? rightValue) && TryGetNot(rightNot, out Expression? rightNotValue) && PropertyMatch(left, rightNotValue))
                    return AndAlso(left, rightValue);
            }

            {
                if (TryGetOr(right, out Expression? rightValue, out Expression? rightNot) && TryGetNot(rightNot, out Expression? rightNotValue) && PropertyMatch(left, rightNotValue))
                    return AndAlso(rightValue, left);
            }

            {
                if (TryGetOr(left, out Expression? leftNot, out Expression? leftValue) && TryGetNot(leftNot, out Expression? leftNotValue) && PropertyMatch(right, leftNotValue))
                    return AndAlso(right, leftValue);
            }
        }

        if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr))
        {
            {
                if (TryGetAnd(rightOr, out Expression? rightNot, out Expression? rightValue) && TryGetNot(rightNot, out Expression? rightNotValue) && PropertyMatch(leftOr, rightNotValue))
                    return OrElse(leftOr, rightValue);
            }

            {
                if (TryGetAnd(rightOr, out Expression? rightValue, out Expression? rightNot) && TryGetNot(rightNot, out Expression? rightNotValue) && PropertyMatch(leftOr, rightNotValue))
                    return OrElse(rightValue, leftOr);
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftNot, out Expression? leftValue) && TryGetNot(leftNot, out Expression? leftNotValue) && PropertyMatch(rightOr, leftNotValue))
                    return OrElse(rightOr, leftValue);
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftValue, out Expression? leftNot) && TryGetNot(leftNot, out Expression? leftNotValue) && PropertyMatch(rightOr, leftNotValue))
                    return OrElse(leftValue, rightOr);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetAnd(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftNotValue, rightValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftNot, out Expression? leftValue)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetAnd(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftNotValue, rightValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftNot, out Expression? leftValue)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetAnd(rightOr, out Expression? rightNot, out Expression? rightValue)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftNotValue, rightValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return CreateBool(true);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftOrNot)
                    && TryGetOr(leftOrNot, out Expression? leftOrLeft, out Expression? leftOrRight)
                    && TryGetAnd(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftOrLeft, rightValue)
                    && PropertyMatch(leftOrRight, rightNotValue))
                    return Not(leftOrRight);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftOrNot)
                    && TryGetOr(leftOrNot, out Expression? leftOrLeft, out Expression? leftOrRight)
                    && TryGetAnd(rightOr, out Expression? rightNot, out Expression? rightValue)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftOrLeft, rightValue)
                    && PropertyMatch(leftOrRight, rightNotValue))
                    return Not(leftOrRight);
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetNot(rightOr, out Expression? rightOrNot)
                    && TryGetOr(rightOrNot, out Expression? rightOrLeft, out Expression? rightOrRight)
                    && PropertyMatch(rightOrLeft, leftValue)
                    && PropertyMatch(rightOrRight, leftNotValue))
                    return Not(rightOrRight);
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftNot, out Expression? leftValue)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetNot(rightOr, out Expression? rightOrNot)
                    && TryGetOr(rightOrNot, out Expression? rightOrLeft, out Expression? rightOrRight)
                    && PropertyMatch(rightOrLeft, leftValue)
                    && PropertyMatch(rightOrRight, leftNotValue))
                    return Not(rightOrRight);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftValue, out Expression? _)
                    && TryGetOr(rightOr, out Expression? rightNot, out Expression? _)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftValue)
                    && TryGetOr(rightOr, out Expression? rightNot, out Expression? _)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftValue, out Expression? _)
                    && TryGetOr(rightOr, out Expression? _, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftValue)
                    && TryGetOr(rightOr, out Expression? _, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftNot, out Expression? _)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetOr(rightOr, out Expression? rightValue, out Expression? _)
                    && PropertyMatch(leftNotValue, rightValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftNot, out Expression? _)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetOr(rightOr, out Expression? _, out Expression? rightValue)
                    && PropertyMatch(leftNotValue, rightValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetOr(rightOr, out Expression? rightValue, out Expression? _)
                    && PropertyMatch(leftNotValue, rightValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetOr(rightOr, out Expression? _, out Expression? rightValue)
                    && PropertyMatch(leftNotValue, rightValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftNot, out Expression? leftValue)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetAnd(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return OrElse(OrElse(Not(leftNotValue), rightValue), leftValue);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetAnd(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return OrElse(leftValue, OrElse(Not(leftNotValue), rightValue));
            }

            {
                if (TryGetOr(leftOr, out Expression? leftNot, out Expression? leftValue)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetAnd(rightOr, out Expression? rightNot, out Expression? rightValue)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return OrElse(OrElse(Not(leftNotValue), rightValue), leftValue);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && TryGetAnd(rightOr, out Expression? rightNot, out Expression? rightValue)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightNotValue))
                    return OrElse(leftValue, OrElse(Not(leftNotValue), rightValue));
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? _)
                    && TryGetOr(rightOr, out Expression? rightNot, out Expression? rightValue)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightValue))
                    return OrElse(leftValue, OrElse(rightValue, Not(rightNotValue)));
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? _)
                    && TryGetOr(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightValue))
                    return OrElse(leftValue, OrElse(rightValue, Not(rightNotValue)));
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftNot, out Expression? leftValue)
                    && TryGetNot(leftNot, out Expression? _)
                    && TryGetOr(rightOr, out Expression? rightNot, out Expression? rightValue)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightValue))
                    return OrElse(leftValue, OrElse(rightValue, Not(rightNotValue)));
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftNot, out Expression? leftValue)
                    && TryGetNot(leftNot, out Expression? _)
                    && TryGetOr(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(leftValue, rightValue))
                    return OrElse(leftValue, OrElse(rightValue, Not(rightNotValue)));
            }

            {
                if (TryGetNot(leftOr, out Expression? leftOrNot)
                    && TryGetOr(leftOrNot, out Expression? leftOrLeft, out Expression? leftOrRight)
                    && TryGetAnd(rightOr, out Expression? rightValue, out Expression? rightNot)
                    && TryGetNot(rightNot, out Expression? _)
                    && PropertyMatch(leftOrLeft, rightValue))
                    return AndAlso(Not(leftOrLeft), OrElse(Not(leftOrRight), rightValue));
            }

            {
                if (TryGetNot(leftOr, out Expression? leftOrNot)
                    && TryGetOr(leftOrNot, out Expression? leftOrLeft, out Expression? leftOrRight)
                    && TryGetAnd(rightOr, out Expression? rightNot, out Expression? rightValue)
                    && TryGetNot(rightNot, out Expression? _)
                    && PropertyMatch(leftOrLeft, rightValue))
                    return AndAlso(Not(leftOrLeft), OrElse(rightValue, Not(leftOrRight)));
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? _)
                    && TryGetNot(rightOr, out Expression? rightOrNot)
                    && TryGetOr(rightOrNot, out Expression? rightOrLeft, out Expression? rightOrRight)
                    && PropertyMatch(rightOrLeft, leftValue))
                    return AndAlso(OrElse(Not(rightOrRight), leftValue), Not(rightOrLeft));
            }

            {
                if (TryGetAnd(leftOr, out Expression? leftValue, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? _)
                    && TryGetNot(rightOr, out Expression? rightOrNot)
                    && TryGetOr(rightOrNot, out Expression? rightOrLeft, out Expression? rightOrRight)
                    && PropertyMatch(rightOrLeft, leftValue))
                    return AndAlso(OrElse(leftValue, Not(rightOrRight)), Not(rightOrLeft));
            }

            {
                if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight)
                    && TryGetNot(rightOr, out Expression? rightOrNot)
                    && TryGetOr(rightOrNot, out Expression? rightOrLeft, out Expression? rightOrRight)
                    && PropertyMatch(leftOrLeft, rightOrLeft))
                    return OrElse(leftOrLeft, OrElse(leftOrRight, Not(rightOrRight)));
            }

            {
                if (TryGetOr(leftOr, out Expression? leftOrLeft, out Expression? leftOrRight)
                    && TryGetNot(rightOr, out Expression? rightOrNot)
                    && TryGetOr(rightOrNot, out Expression? rightOrLeft, out Expression? rightOrRight)
                    && PropertyMatch(leftOrLeft, rightOrLeft))
                    return OrElse(leftOrRight, OrElse(leftOrLeft, Not(rightOrRight)));
            }

            {
                if (TryGetNot(leftOr, out Expression? leftOrNot)
                    && TryGetOr(leftOrNot, out Expression? leftOrLeft, out Expression? leftOrRight)
                    && TryGetOr(rightOr, out Expression? rightOrLeft, out Expression? rightOrRight)
                    && PropertyMatch(rightOrLeft, leftOrLeft))
                    return OrElse(Not(leftOrRight), OrElse(rightOrLeft, rightOrRight));
            }
        }

        return expr;
    }

    internal static Expression AssociateComplement(Expression expr)
    {
        // !(a && b) && a -> a && !b
        // !(a && b) && b -> b && !a
        // (a && x) && !a -> false
        // !(a || b) || a -> a || !b
        // (a || x) || !a -> true

        if (TryGetAnd(expr, out Expression? left, out Expression? right))
        {
            {
                if (TryGetNot(left, out Expression? leftNot)
                    && TryGetAnd(leftNot, out Expression? leftInner, out Expression? leftInnerRight)
                    && PropertyMatch(leftInner, right))
                    return AndAlso(leftInner, Not(leftInnerRight));
            }

            {
                if (TryGetNot(left, out Expression? leftNot)
                    && TryGetAnd(leftNot, out Expression? leftInnerLeft, out Expression? leftInnerRight)
                    && PropertyMatch(leftInnerRight, right))
                    return AndAlso(leftInnerRight, Not(leftInnerLeft));
            }

            {
                if (TryGetNot(right, out Expression? rightNot)
                    && TryGetAnd(rightNot, out Expression? rightInner, out Expression? rightInnerRight)
                    && PropertyMatch(rightInner, left))
                    return AndAlso(rightInner, Not(rightInnerRight));
            }

            {
                if (TryGetNot(right, out Expression? rightNot)
                    && TryGetAnd(rightNot, out Expression? rightInnerLeft, out Expression? rightInnerRight)
                    && PropertyMatch(rightInnerRight, left))
                    return AndAlso(rightInnerRight, Not(rightInnerLeft));
            }

            {
                if (TryGetAnd(left, out Expression? leftInner, out Expression? _)
                    && TryGetNot(right, out Expression? rightNot)
                    && PropertyMatch(leftInner, rightNot))
                    return CreateBool(false);
            }

            {
                if (TryGetAnd(left, out Expression? leftNot, out Expression? _)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && PropertyMatch(leftNotValue, right))
                    return CreateBool(false);
            }

            {
                if (TryGetNot(left, out Expression? leftNotValue)
                    && TryGetAnd(right, out Expression? rightInner, out Expression? _)
                    && PropertyMatch(leftNotValue, rightInner))
                    return CreateBool(false);
            }

            {
                if (TryGetAnd(right, out Expression? rightNot, out Expression? _)
                    && TryGetNot(rightNot, out Expression? rightNotValue)
                    && PropertyMatch(rightNotValue, left))
                    return CreateBool(false);
            }
        }

        if (TryGetOr(expr, out Expression? leftOr, out Expression? rightOr))
        {
            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(leftNot, out Expression? leftValue, out Expression? rightValue)
                    && PropertyMatch(rightValue, rightOr))
                    return OrElse(Not(leftValue), rightOr);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(leftNot, out Expression? leftValue, out Expression? rightValue)
                    && PropertyMatch(leftValue, rightOr))
                    return OrElse(rightOr, Not(rightValue));
            }

            {
                if (TryGetNot(rightOr, out Expression? rightNot)
                    && TryGetOr(rightNot, out Expression? leftValue, out Expression? rightValue)
                    && PropertyMatch(leftValue, leftOr))
                    return OrElse(leftOr, Not(rightValue));
            }

            {
                if (TryGetNot(rightOr, out Expression? rightNot)
                    && TryGetOr(rightNot, out Expression? leftValue, out Expression? rightValue)
                    && PropertyMatch(leftValue, leftOr))
                    return OrElse(leftOr, Not(rightValue));
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftValue)
                    && TryGetNot(rightOr, out Expression? rightNot)
                    && PropertyMatch(leftValue, rightNot))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftValue, out Expression? _)
                    && TryGetNot(rightOr, out Expression? rightNot)
                    && PropertyMatch(leftValue, rightNot))
                    return CreateBool(true);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(rightOr, out Expression? _, out Expression? rightValue)
                    && PropertyMatch(leftNot, rightValue))
                    return CreateBool(true);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(rightOr, out Expression? rightValue, out Expression? _)
                    && PropertyMatch(leftNot, rightValue))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftInner)
                    && TryGetOr(leftInner, out Expression? _, out Expression? leftInnerRight)
                    && TryGetNot(rightOr, out Expression? rightNot)
                    && PropertyMatch(leftInnerRight, rightNot))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftInner)
                    && TryGetOr(leftInner, out Expression? leftInnerLeft, out Expression? _)
                    && TryGetNot(rightOr, out Expression? rightNot)
                    && PropertyMatch(leftInnerLeft, rightNot))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftInner, out Expression? _)
                    && TryGetOr(leftInner, out Expression? leftInnerLeft, out Expression? _)
                    && TryGetNot(rightOr, out Expression? rightNot)
                    && PropertyMatch(leftInnerLeft, rightNot))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftInner, out Expression? _)
                    && TryGetOr(leftInner, out Expression? _, out Expression? leftInnerRight)
                    && TryGetNot(rightOr, out Expression? rightNot)
                    && PropertyMatch(leftInnerRight, rightNot))
                    return CreateBool(true);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(rightOr, out Expression? _, out Expression? rightInner)
                    && TryGetOr(rightInner, out Expression? _, out Expression? rightInnerRight)
                    && PropertyMatch(leftNot, rightInnerRight))
                    return CreateBool(true);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(rightOr, out Expression? _, out Expression? rightInner)
                    && TryGetOr(rightInner, out Expression? rightInnerLeft, out Expression? _)
                    && PropertyMatch(leftNot, rightInnerLeft))
                    return CreateBool(true);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(rightOr, out Expression? rightInner, out Expression? _)
                    && TryGetOr(rightInner, out Expression? rightInnerLeft, out Expression? _)
                    && PropertyMatch(leftNot, rightInnerLeft))
                    return CreateBool(true);
            }

            {
                if (TryGetNot(leftOr, out Expression? leftNot)
                    && TryGetOr(rightOr, out Expression? rightInner, out Expression? _)
                    && TryGetOr(rightInner, out Expression? _, out Expression? rightInnerRight)
                    && PropertyMatch(leftNot, rightInnerRight))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftInner, out Expression? _)
                    && TryGetOr(leftInner, out Expression? _, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && PropertyMatch(leftNotValue, rightOr))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? leftInner, out Expression? _)
                    && TryGetOr(leftInner, out Expression? leftNot, out Expression? _)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && PropertyMatch(leftNotValue, rightOr))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftInner)
                    && TryGetOr(leftInner, out Expression? _, out Expression? leftNot)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && PropertyMatch(leftNotValue, rightOr))
                    return CreateBool(true);
            }

            {
                if (TryGetOr(leftOr, out Expression? _, out Expression? leftInner)
                    && TryGetOr(leftInner, out Expression? leftNot, out Expression? _)
                    && TryGetNot(leftNot, out Expression? leftNotValue)
                    && PropertyMatch(leftNotValue, rightOr))
                    return CreateBool(true);
            }
        }

        return expr;
    }

    internal static Expression DoubleNegation(Expression expr)
    {
        // !!x -> x

        if (TryGetNot(expr, out Expression? inner) && TryGetNot(inner, out Expression? inner2))
            return inner2;

        return expr;
    }

    internal static Expression DeMorgan(Expression expr)
    {
        // !a || !b -> !(a && b)
        // !a && !b -> !(a || b)

        {
            if (TryGetOr(expr, out Expression? left, out Expression? right) && TryGetNot(left, out Expression? leftNot) && TryGetNot(right, out Expression? rightNot))
                return Not(AndAlso(leftNot, rightNot));
        }

        {
            if (TryGetAnd(expr, out Expression? leftAnd, out Expression? rightAnd) && TryGetNot(leftAnd, out Expression? leftNot) && TryGetNot(rightAnd, out Expression? rightNot))
                return Not(OrElse(leftNot, rightNot));
        }

        return expr;
    }

    internal static Expression BalanceTree(Expression expression)
    {
        // a || (b || (c || (d || (e || (f || (g || h)))))) -> ((a || b) || (c || d)) || ((e || f) || (g || h))
        // a && (b && (c && (d && (e && (f && (g && h)))))) -> ((a && b) && (c && d)) && ((e && f) && (g && h))

        if (TryGetOr(expression, out Expression? a, out Expression? or2)
            && TryGetOr(or2, out Expression? b, out Expression? or3)
            && TryGetOr(or3, out Expression? c, out Expression? or4)
            && TryGetOr(or4, out Expression? d, out Expression? or5)
            && TryGetOr(or5, out Expression? e, out Expression? or6)
            && TryGetOr(or6, out Expression? f, out Expression? or7)
            && TryGetOr(or7, out Expression? g, out Expression? h))
            return OrElse(OrElse(OrElse(a, b), OrElse(c, d)),
                OrElse(OrElse(e, f), OrElse(g, h)));

        {
            if (TryGetOr(expression, out Expression? left1, out Expression? right1)
                && TryGetOr(left1, out Expression? left2, out Expression? right2)
                && TryGetOr(left2, out Expression? left3, out Expression? right3)
                && TryGetOr(left3, out Expression? left4, out Expression? right4)
                && TryGetOr(left4, out Expression? left5, out Expression? right5)
                && TryGetOr(left5, out Expression? left6, out Expression? right6)
                && TryGetOr(left6, out Expression? leftInnerLeft, out Expression? leftInnerRight))
                return OrElse(OrElse(OrElse(leftInnerLeft, leftInnerRight), OrElse(right6, right5)),
                    OrElse(OrElse(right4, right3), OrElse(right2, right1)));
        }

        if (TryGetAnd(expression, out Expression? a1, out Expression? and2)
            && TryGetAnd(and2, out Expression? a2, out Expression? and3)
            && TryGetAnd(and3, out Expression? a3, out Expression? and4)
            && TryGetAnd(and4, out Expression? a4, out Expression? and5)
            && TryGetAnd(and5, out Expression? a5, out Expression? and6)
            && TryGetAnd(and6, out Expression? a6, out Expression? and7)
            && TryGetAnd(and7, out Expression? a7, out Expression? a8))
            return AndAlso(AndAlso(AndAlso(a1, a2), AndAlso(a3, a4)),
                AndAlso(AndAlso(a5, a6), AndAlso(a7, a8)));

        {
            if (TryGetAnd(expression, out Expression? leftA1, out Expression? rightA1)
                && TryGetAnd(leftA1, out Expression? leftA2, out Expression? rightA2)
                && TryGetAnd(leftA2, out Expression? leftA3, out Expression? rightA3)
                && TryGetAnd(leftA3, out Expression? leftA4, out Expression? rightA4)
                && TryGetAnd(leftA4, out Expression? leftA5, out Expression? rightA5)
                && TryGetAnd(leftA5, out Expression? leftA6, out Expression? rightA6)
                && TryGetAnd(leftA6, out Expression? leftInnerLeft, out Expression? leftInnerRight))
                return AndAlso(AndAlso(AndAlso(leftInnerLeft, leftInnerRight), AndAlso(rightA6, rightA5)),
                    AndAlso(AndAlso(rightA4, rightA3), AndAlso(rightA2, rightA1)));
        }

        return expression;
    }

    private static bool MatchesEither(Expression candidate, Expression left, Expression right) => PropertyMatch(candidate, left) || PropertyMatch(candidate, right);

    private static bool IsComplement(Expression candidate, Expression other) => TryGetNot(candidate, out Expression? inner) && PropertyMatch(inner, other);

    private static bool IsComplementPair(Expression left, Expression right) => IsComplement(left, right) || IsComplement(right, left);

    private static bool TryGetComplementAndRest(Expression left, Expression right, [NotNullWhen(true)]out Expression? rest)
    {
        rest = null;

        if (!TryGetAnd(left, out Expression? leftFirst, out Expression? leftSecond) || !TryGetAnd(right, out Expression? rightFirst, out Expression? rightSecond))
            return false;

        if (IsComplementPair(leftFirst, rightFirst) && PropertyMatch(leftSecond, rightSecond))
        {
            rest = leftSecond;
            return true;
        }

        if (IsComplementPair(leftSecond, rightSecond) && PropertyMatch(leftFirst, rightFirst))
        {
            rest = leftFirst;
            return true;
        }

        return false;
    }
}
