using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Generators.Expressions.Optimizer.Helpers;
using static Genbox.FastData.Generators.Expressions.Optimizer.Helpers.ReductionHelpers;

namespace Genbox.FastData.Generators.Expressions.Optimizer;

internal static class ComparisonReduction
{
    internal static Expression ReplaceConstantComparison(Expression expr)
    {
        // 5 < 7 -> true
        // 3 == 4 -> false
        // 1.0 >= 2.0 -> false

        if (expr is not BinaryExpression be
            || !ExprHelpers.TryGetConstantComparable(be.Left, out IComparable? left)
            || !ExprHelpers.TryGetConstantComparable(be.Right, out IComparable? right)
            || left.GetType() != right.GetType())
            return expr;

        if (ExprHelpers.TryGetFloatingComparison(expr.NodeType, left, right, out bool res))
            return CreateBool(res);

        return expr.NodeType switch
        {
            ExpressionType.Equal => CreateBool(Equals(left, right)),
            ExpressionType.LessThan => CreateBool(left.CompareTo(right) < 0),
            ExpressionType.LessThanOrEqual => CreateBool(left.CompareTo(right) <= 0),
            ExpressionType.GreaterThan => CreateBool(left.CompareTo(right) > 0),
            ExpressionType.GreaterThanOrEqual => CreateBool(left.CompareTo(right) >= 0),
            ExpressionType.NotEqual => CreateBool(!Equals(left, right)),
            _ => expr
        };
    }

    internal static Expression SimplifyComparison(Expression expr)
    {
        // x <= x -> true
        // x < x -> false
        // min <= y -> true
        // max < y -> false
        // x >= min -> true
        // x > max -> false
        if (expr is not BinaryExpression be)
            return expr;

        if (expr.NodeType is not (ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual)
            || be.Left.Type != be.Right.Type)
            return expr;

        Type type = be.Left.Type;
        if (!IsIntegralType(type))
            return expr;

        if (ExprHelpers.PropertyMatch(be.Left, be.Right))
            return CreateBool(expr.NodeType is ExpressionType.LessThanOrEqual or ExpressionType.GreaterThanOrEqual);

        if (be.Left is ConstantExpression lConst)
        {
            if (IsMinValue(lConst))
            {
                if (expr.NodeType == ExpressionType.LessThanOrEqual)
                    return CreateBool(true);
                if (expr.NodeType == ExpressionType.GreaterThan)
                    return CreateBool(false);
            }

            if (IsMaxValue(lConst))
            {
                if (expr.NodeType == ExpressionType.GreaterThanOrEqual)
                    return CreateBool(true);
                if (expr.NodeType == ExpressionType.LessThan)
                    return CreateBool(false);
            }
        }

        if (be.Right is ConstantExpression rConst)
        {
            if (IsMinValue(rConst))
            {
                if (expr.NodeType == ExpressionType.GreaterThanOrEqual)
                    return CreateBool(true);
                if (expr.NodeType == ExpressionType.LessThan)
                    return CreateBool(false);
            }

            if (IsMaxValue(rConst))
            {
                if (expr.NodeType == ExpressionType.LessThanOrEqual)
                    return CreateBool(true);
                if (expr.NodeType == ExpressionType.GreaterThan)
                    return CreateBool(false);
            }
        }

        return expr;
    }

    internal static Expression OptimizeComparisonRanges(Expression expression)
    {
        if (TryGetAnd(expression, out Expression? left, out Expression? right)
            && TryGetComparisonExpression(left, out ExpressionType leftOp, out Expression? leftVar, out Expression? leftVal)
            && TryGetComparisonExpression(right, out ExpressionType rightOp, out Expression? rightVar, out Expression? rightVal)
            && ExprHelpers.PropertyMatch(leftVar, rightVar))
        {
            if (IsFloatingPointType(leftVar.Type) || IsFloatingPointType(rightVar.Type))
                return expression;

            switch (leftOp, rightOp)
            {
                case (ExpressionType.GreaterThan, ExpressionType.GreaterThan):
                    if (TryCompareConstants(leftVal, rightVal, out int cmpGt))
                    {
                        if (cmpGt > 0)
                            return MakeBinary(ExpressionType.GreaterThan, leftVar, leftVal);
                        if (cmpGt < 0)
                            return MakeBinary(ExpressionType.GreaterThan, rightVar, rightVal);
                        if (ExprHelpers.PropertyMatch(leftVal, rightVal))
                            return MakeBinary(ExpressionType.GreaterThan, leftVar, leftVal);
                    }
                    return expression;
                case (ExpressionType.GreaterThanOrEqual, ExpressionType.GreaterThanOrEqual):
                    if (TryCompareConstants(leftVal, rightVal, out int cmpGte))
                    {
                        if (cmpGte > 0)
                            return MakeBinary(ExpressionType.GreaterThanOrEqual, leftVar, leftVal);
                        if (cmpGte < 0)
                            return MakeBinary(ExpressionType.GreaterThanOrEqual, rightVar, rightVal);
                        if (ExprHelpers.PropertyMatch(leftVal, rightVal))
                            return MakeBinary(ExpressionType.GreaterThanOrEqual, leftVar, leftVal);
                    }
                    return expression;
                case (ExpressionType.LessThan, ExpressionType.LessThan):
                    if (TryCompareConstants(leftVal, rightVal, out int cmpLt))
                    {
                        if (cmpLt < 0)
                            return MakeBinary(ExpressionType.LessThan, leftVar, leftVal);
                        if (cmpLt > 0)
                            return MakeBinary(ExpressionType.LessThan, rightVar, rightVal);
                        if (ExprHelpers.PropertyMatch(leftVal, rightVal))
                            return MakeBinary(ExpressionType.LessThan, leftVar, leftVal);
                    }
                    return expression;
                case (ExpressionType.LessThanOrEqual, ExpressionType.LessThanOrEqual):
                    if (TryCompareConstants(leftVal, rightVal, out int cmpLte))
                    {
                        if (cmpLte < 0)
                            return MakeBinary(ExpressionType.LessThanOrEqual, leftVar, leftVal);
                        if (cmpLte > 0)
                            return MakeBinary(ExpressionType.LessThanOrEqual, rightVar, rightVal);
                        if (ExprHelpers.PropertyMatch(leftVal, rightVal))
                            return MakeBinary(ExpressionType.LessThanOrEqual, leftVar, leftVal);
                    }
                    return expression;
                case (ExpressionType.GreaterThanOrEqual, ExpressionType.LessThanOrEqual):
                case (ExpressionType.LessThanOrEqual, ExpressionType.GreaterThanOrEqual):
                    if (ExprHelpers.PropertyMatch(leftVal, rightVal))
                        return MakeBinary(ExpressionType.Equal, leftVar, leftVal);
                    return expression;
                case (ExpressionType.GreaterThan, ExpressionType.LessThan):
                case (ExpressionType.GreaterThan, ExpressionType.LessThanOrEqual):
                case (ExpressionType.LessThan, ExpressionType.GreaterThan):
                case (ExpressionType.LessThan, ExpressionType.GreaterThanOrEqual):
                case (ExpressionType.GreaterThanOrEqual, ExpressionType.LessThan):
                case (ExpressionType.LessThanOrEqual, ExpressionType.GreaterThan):
                    if (ExprHelpers.PropertyMatch(leftVal, rightVal))
                        return Constant(false, typeof(bool));
                    return expression;
                default:
                    return expression;
            }
        }

        if (TryGetOr(expression, out Expression? leftOr, out Expression? rightOr)
            && TryGetComparisonExpression(leftOr, out ExpressionType leftOpOr, out Expression? leftVarOr, out Expression? leftValOr)
            && TryGetComparisonExpression(rightOr, out ExpressionType rightOpOr, out Expression? rightVarOr, out Expression? rightValOr)
            && ExprHelpers.PropertyMatch(leftVarOr, rightVarOr))
        {
            if (IsFloatingPointType(leftVarOr.Type) || IsFloatingPointType(rightVarOr.Type))
                return expression;

            switch (leftOpOr, rightOpOr)
            {
                case (ExpressionType.GreaterThan, ExpressionType.GreaterThan):
                    if (TryCompareConstants(leftValOr, rightValOr, out int cmpGt))
                    {
                        if (cmpGt > 0)
                            return MakeBinary(ExpressionType.GreaterThan, rightVarOr, rightValOr);
                        if (cmpGt < 0)
                            return MakeBinary(ExpressionType.GreaterThan, leftVarOr, leftValOr);
                        if (ExprHelpers.PropertyMatch(leftValOr, rightValOr))
                            return MakeBinary(ExpressionType.GreaterThan, leftVarOr, leftValOr);
                    }
                    return expression;
                case (ExpressionType.GreaterThanOrEqual, ExpressionType.GreaterThanOrEqual):
                    if (TryCompareConstants(leftValOr, rightValOr, out int cmpGte))
                    {
                        if (cmpGte > 0)
                            return MakeBinary(ExpressionType.GreaterThanOrEqual, rightVarOr, rightValOr);
                        if (cmpGte < 0)
                            return MakeBinary(ExpressionType.GreaterThanOrEqual, leftVarOr, leftValOr);
                        if (ExprHelpers.PropertyMatch(leftValOr, rightValOr))
                            return MakeBinary(ExpressionType.GreaterThanOrEqual, leftVarOr, leftValOr);
                    }
                    return expression;
                case (ExpressionType.LessThan, ExpressionType.LessThan):
                    if (TryCompareConstants(leftValOr, rightValOr, out int cmpLt))
                    {
                        if (cmpLt < 0)
                            return MakeBinary(ExpressionType.LessThan, rightVarOr, rightValOr);
                        if (cmpLt > 0)
                            return MakeBinary(ExpressionType.LessThan, leftVarOr, leftValOr);
                        if (ExprHelpers.PropertyMatch(leftValOr, rightValOr))
                            return MakeBinary(ExpressionType.LessThan, leftVarOr, leftValOr);
                    }
                    return expression;
                case (ExpressionType.LessThanOrEqual, ExpressionType.LessThanOrEqual):
                    if (TryCompareConstants(leftValOr, rightValOr, out int cmpLte))
                    {
                        if (cmpLte < 0)
                            return MakeBinary(ExpressionType.LessThanOrEqual, rightVarOr, rightValOr);
                        if (cmpLte > 0)
                            return MakeBinary(ExpressionType.LessThanOrEqual, leftVarOr, leftValOr);
                        if (ExprHelpers.PropertyMatch(leftValOr, rightValOr))
                            return MakeBinary(ExpressionType.LessThanOrEqual, leftVarOr, leftValOr);
                    }
                    return expression;
                case (ExpressionType.GreaterThanOrEqual, ExpressionType.LessThanOrEqual):
                case (ExpressionType.LessThanOrEqual, ExpressionType.GreaterThanOrEqual):
                case (ExpressionType.GreaterThanOrEqual, ExpressionType.LessThan):
                case (ExpressionType.LessThanOrEqual, ExpressionType.GreaterThan):
                case (ExpressionType.GreaterThan, ExpressionType.LessThanOrEqual):
                case (ExpressionType.LessThan, ExpressionType.GreaterThanOrEqual):
                    if (ExprHelpers.PropertyMatch(leftValOr, rightValOr))
                        return Constant(true, typeof(bool));
                    return expression;
                case (ExpressionType.GreaterThan, ExpressionType.LessThan):
                case (ExpressionType.LessThan, ExpressionType.GreaterThan):
                    if (ExprHelpers.PropertyMatch(leftValOr, rightValOr))
                        return MakeBinary(ExpressionType.NotEqual, leftVarOr, leftValOr);
                    return expression;
                default:
                    return expression;
            }
        }

        return expression;
    }

    internal static Expression CompareEqualityWithItself(Expression expression)
    {
        if (TryGetComparisonExpression(expression, out ExpressionType op, out Expression? leftVar, out Expression? rightVar))
        {
            if (IsFloatingPointType(leftVar.Type))
                return expression;

            if (IsFloatingPointType(rightVar.Type))
                return expression;

            if (op == ExpressionType.Equal && ExprHelpers.PropertyMatch(leftVar, rightVar))
                return Constant(true, typeof(bool));

            if (op == ExpressionType.NotEqual && ExprHelpers.PropertyMatch(leftVar, rightVar))
                return Constant(false, typeof(bool));
        }

        return expression;
    }

    internal static Expression RemoveDuplicateCondition(Expression expression)
    {
        const int comparisonDepth = 2;

        if (TryGetAnd(expression, out Expression? left, out Expression? right))
        {
            if (TryGetComparisonExpression(left, out ExpressionType leftOp, out Expression? leftA, out Expression? leftB)
                && ContainsMatchingComparison(right, leftOp, leftA, leftB, TryGetAnd, comparisonDepth))
                return left;

            if (TryGetComparisonExpression(right, out ExpressionType rightOp, out Expression? rightA, out Expression? rightB)
                && ContainsMatchingComparison(left, rightOp, rightA, rightB, TryGetAnd, comparisonDepth))
                return left;
        }

        if (TryGetOr(expression, out Expression? leftOr, out Expression? rightOr))
        {
            if (TryGetComparisonExpression(leftOr, out ExpressionType leftOp, out Expression? leftA, out Expression? leftB)
                && ContainsMatchingComparison(rightOr, leftOp, leftA, leftB, TryGetOr, comparisonDepth))
                return leftOr;

            if (TryGetComparisonExpression(rightOr, out ExpressionType rightOp, out Expression? rightA, out Expression? rightB)
                && ContainsMatchingComparison(leftOr, rightOp, rightA, rightB, TryGetOr, comparisonDepth))
                return leftOr;
        }

        return expression;
    }

    internal static Expression RemoveMutuallyExclusiveCondition(Expression expression)
    {
        const int comparisonDepth = 2;

        if (TryGetAnd(expression, out Expression? left, out Expression? right))
        {
            if (TryGetComparisonExpression(left, out ExpressionType leftOp, out Expression? leftA, out Expression? leftB)
                && ContainsOpposingComparison(right, leftOp, leftA, leftB, TryGetAnd, comparisonDepth))
                return Constant(false, typeof(bool));

            if (TryGetComparisonExpression(right, out ExpressionType rightOp, out Expression? rightA, out Expression? rightB)
                && ContainsOpposingComparison(left, rightOp, rightA, rightB, TryGetAnd, comparisonDepth))
                return Constant(false, typeof(bool));
        }

        if (TryGetOr(expression, out Expression? leftOr, out Expression? rightOr))
        {
            if (TryGetComparisonExpression(leftOr, out ExpressionType leftOp, out Expression? leftA, out Expression? leftB)
                && ContainsOpposingComparison(rightOr, leftOp, leftA, leftB, TryGetOr, comparisonDepth))
                return Constant(true, typeof(bool));

            if (TryGetComparisonExpression(rightOr, out ExpressionType rightOp, out Expression? rightA, out Expression? rightB)
                && ContainsOpposingComparison(leftOr, rightOp, rightA, rightB, TryGetOr, comparisonDepth))
                return Constant(true, typeof(bool));
        }

        return expression;
    }

    private delegate bool TryGetBinary(Expression expression, [NotNullWhen(true)]out Expression? left, [NotNullWhen(true)]out Expression? right);

    private static bool TryCompareConstants(Expression left, Expression right, out int comparison)
    {
        comparison = 0;
        if (left is ConstantExpression lCe && right is ConstantExpression rCe)
        {
            if (lCe.Type != rCe.Type || lCe.Value == null || rCe.Value == null)
                return false;

            if (lCe.Value is double ld && rCe.Value is double rd)
            {
                if (double.IsNaN(ld) || double.IsNaN(rd))
                    return false;
            }

            if (lCe.Value is float lf && rCe.Value is float rf)
            {
                if (float.IsNaN(lf) || float.IsNaN(rf))
                    return false;
            }

            if (lCe.Value is IComparable lComp && rCe.Value is IComparable rComp)
            {
                comparison = lComp.CompareTo(rComp);
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMatchingComparison(
        Expression expression,
        ExpressionType op,
        Expression left,
        Expression right,
        TryGetBinary tryGetBinary,
        int depth)
    {
        if (TryGetComparisonExpression(expression, out ExpressionType foundOp, out Expression? foundLeft, out Expression? foundRight)
            && foundOp == op
            && ExprHelpers.PropertyMatch(foundLeft, left)
            && ExprHelpers.PropertyMatch(foundRight, right))
            return true;

        if (depth == 0)
            return false;

        if (tryGetBinary(expression, out Expression? childLeft, out Expression? childRight))
        {
            if (ContainsMatchingComparison(childLeft, op, left, right, tryGetBinary, depth - 1))
                return true;

            if (ContainsMatchingComparison(childRight, op, left, right, tryGetBinary, depth - 1))
                return true;
        }

        return false;
    }

    private static bool ContainsOpposingComparison(
        Expression expression,
        ExpressionType leftOp,
        Expression leftA,
        Expression leftB,
        TryGetBinary tryGetBinary,
        int depth)
    {
        if (TryGetComparisonExpression(expression, out ExpressionType rightOp, out Expression? rightA, out Expression? rightB)
            && CheckMatch(leftOp, leftA, leftB, rightOp, rightA, rightB))
            return true;

        if (depth == 0)
            return false;

        if (tryGetBinary(expression, out Expression? childLeft, out Expression? childRight))
        {
            if (ContainsOpposingComparison(childLeft, leftOp, leftA, leftB, tryGetBinary, depth - 1))
                return true;

            if (ContainsOpposingComparison(childRight, leftOp, leftA, leftB, tryGetBinary, depth - 1))
                return true;
        }

        return false;
    }

    private static bool CheckMatch(ExpressionType leftOp,
                                   Expression leftA,
                                   Expression leftB,
                                   ExpressionType rightOp,
                                   Expression rightA,
                                   Expression rightB)
    {
        bool oppositeEquality =
            (leftOp == ExpressionType.Equal && rightOp == ExpressionType.NotEqual)
            || (leftOp == ExpressionType.NotEqual && rightOp == ExpressionType.Equal);

        if (oppositeEquality && ((ExprHelpers.PropertyMatch(leftA, rightA) && ExprHelpers.PropertyMatch(leftB, rightB)) ||
                                 (ExprHelpers.PropertyMatch(leftA, rightB) && ExprHelpers.PropertyMatch(leftB, rightA))))
            return true;

        bool oppositeRange =
            (leftOp == ExpressionType.LessThan && rightOp == ExpressionType.GreaterThanOrEqual)
            || (leftOp == ExpressionType.GreaterThan && rightOp == ExpressionType.LessThanOrEqual)
            || (leftOp == ExpressionType.GreaterThanOrEqual && rightOp == ExpressionType.LessThan)
            || (leftOp == ExpressionType.LessThanOrEqual && rightOp == ExpressionType.GreaterThan);

        if (oppositeRange && ExprHelpers.PropertyMatch(leftA, rightA) && ExprHelpers.PropertyMatch(leftB, rightB))
            return true;

        return false;
    }
}
