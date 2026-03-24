using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Expressions.Optimizer;

internal static class ExpressionOptimizer
{
    private static readonly Func<Expression, Expression>[] ConstantReductions =
    [
        ConstantReduction.FoldConstants,
        ConstantReduction.EvaluateConstantMath,
        ConstantReduction.SimplifyConditionals,
        ConstantReduction.SimplifyArithmeticIdentities,
        ConstantReduction.SimplifyArithmeticReductions,
        ConstantReduction.SimplifyBitwise,
        ConstantReduction.SimplifyShift,
        ConstantReduction.ReduceStaticConditional
    ];

    private static readonly Func<Expression, Expression>[] ComparisonReductions =
    [
        ComparisonReduction.RemoveDuplicateCondition,
        ComparisonReduction.RemoveMutuallyExclusiveCondition,
        ComparisonReduction.OptimizeComparisonRanges,
        ComparisonReduction.CompareEqualityWithItself,
        ComparisonReduction.ReplaceConstantComparison,
        ComparisonReduction.SimplifyComparison
    ];

    private static readonly Func<Expression, Expression>[] BooleanAlgebraReductions =
    [
        BoolAlgebraReduction.AssociateComplement,
        BoolAlgebraReduction.Commute,
        BoolAlgebraReduction.CommuteAbsorb,
        BoolAlgebraReduction.DistributeComplement,
        BoolAlgebraReduction.FactorComplement,
        BoolAlgebraReduction.Gather,
        BoolAlgebraReduction.Identity,
        BoolAlgebraReduction.SimplifyNotBool,
        BoolAlgebraReduction.SimplifyBoolean,
        BoolAlgebraReduction.Annihilate,
        BoolAlgebraReduction.Absorb,
        BoolAlgebraReduction.Idempotence,
        BoolAlgebraReduction.Complement,
        BoolAlgebraReduction.DoubleNegation,
        BoolAlgebraReduction.DeMorgan,
        BoolAlgebraReduction.BalanceTree
    ];

    private static List<Func<Expression, Expression>> AllMethods { get; } =
    [
        ..ConstantReductions,
        ..ComparisonReductions,
        ..BooleanAlgebraReductions
    ];

    public static Expression Visit(Expression exp)
    {
        return DoReduction(VisitChildren(exp));
    }

    private static Expression DoReduction(Expression exp)
    {
        Expression current = exp;
        while (true)
        {
            Expression optimized = current;
            foreach (Func<Expression, Expression> method in AllMethods)
                optimized = method(optimized);

            if (ReferenceEquals(optimized, current))
                return current;

            current = optimized;
        }
    }

    private static MemberBinding VisitBinding(MemberBinding binding)
    {
        if (binding is MemberAssignment assignment)
        {
            Expression visited = Visit(assignment.Expression);
            if (visited == assignment.Expression)
                return assignment;

            return Bind(assignment.Member, visited);
        }

        if (binding is MemberMemberBinding memberBinding)
        {
            int count = memberBinding.Bindings.Count;
            MemberBinding[] visitedBindings = new MemberBinding[count];
            bool changed = false;
            for (int i = 0; i < count; i++)
            {
                MemberBinding child = memberBinding.Bindings[i];
                MemberBinding visited = VisitBinding(child);
                visitedBindings[i] = visited;
                if (!ReferenceEquals(visited, child))
                    changed = true;
            }

            if (!changed)
                return memberBinding;

            return MemberBind(memberBinding.Member, visitedBindings);
        }

        if (binding is MemberListBinding listBinding)
        {
            int count = listBinding.Initializers.Count;
            ElementInit[] visitedInitializers = new ElementInit[count];
            bool changed = false;
            for (int i = 0; i < count; i++)
            {
                ElementInit initializer = listBinding.Initializers[i];
                ElementInit visited = VisitElementInit(initializer);
                visitedInitializers[i] = visited;
                if (!ReferenceEquals(visited, initializer))
                    changed = true;
            }

            if (!changed)
                return listBinding;

            return ListBind(listBinding.Member, visitedInitializers);
        }

        return binding;
    }

    private static ElementInit VisitElementInit(ElementInit initializer)
    {
        int count = initializer.Arguments.Count;
        Expression[] visitedArguments = new Expression[count];
        bool changed = false;
        for (int i = 0; i < count; i++)
        {
            Expression argument = initializer.Arguments[i];
            Expression visited = Visit(argument);
            visitedArguments[i] = visited;
            if (!ReferenceEquals(visited, argument))
                changed = true;
        }

        if (!changed)
            return initializer;

        return ElementInit(initializer.AddMethod, visitedArguments);
    }

    private static Expression VisitChildren(Expression expression)
    {
        if (expression.NodeType == ExpressionType.Coalesce && expression is BinaryExpression coalesce && coalesce.Conversion != null)
        {
            Expression left = Visit(coalesce.Left);
            Expression right = Visit(coalesce.Right);
            Expression conversion = Visit(coalesce.Conversion);

            if (left == coalesce.Left && right == coalesce.Right && conversion == coalesce.Conversion)
                return coalesce;

            return Coalesce(left, right, (LambdaExpression)conversion);
        }

        switch (expression.NodeType)
        {
            case ExpressionType.Constant:
                return (ConstantExpression)expression;
            case ExpressionType.Parameter:
                return expression;
            case ExpressionType.MemberAccess:
            {
                MemberExpression member = (MemberExpression)expression;
                if (member.Expression == null)
                    return member;

                Expression visited = Visit(member.Expression);
                if (visited == member.Expression)
                    return member;

                return MakeMemberAccess(visited, member.Member);
            }
            case ExpressionType.Call:
            {
                MethodCallExpression call = (MethodCallExpression)expression;
                Expression? obj = call.Object == null ? null : Visit(call.Object);
                Expression[] args = call.Arguments.ToArray();
                Expression[] visitedArgs = new Expression[args.Length];
                for (int i = 0; i < args.Length; i++)
                    visitedArgs[i] = Visit(args[i]);

                if (call.Object == obj && args.SequenceEqual(visitedArgs))
                    return call;

                return Call(obj, call.Method, visitedArgs);
            }
            case ExpressionType.Lambda:
            {
                LambdaExpression lambda = (LambdaExpression)expression;
                Expression body = Visit(lambda.Body);
                if (body == lambda.Body)
                    return lambda;

                return Lambda(lambda.Type, Visit(body), lambda.Parameters);
            }
            case ExpressionType.TypeIs:
            {
                TypeBinaryExpression typeBinary = (TypeBinaryExpression)expression;
                Expression visited = Visit(typeBinary.Expression);
                if (visited == typeBinary.Expression)
                    return typeBinary;

                return TypeIs(visited, typeBinary.TypeOperand);
            }
            case ExpressionType.Conditional:
            {
                ConditionalExpression conditional = (ConditionalExpression)expression;
                Expression test = Visit(conditional.Test);
                Expression ifTrue = Visit(conditional.IfTrue);
                Expression ifFalse = Visit(conditional.IfFalse);

                if (test == conditional.Test && ifTrue == conditional.IfTrue && ifFalse == conditional.IfFalse)
                    return conditional;

                return Condition(test, ifTrue, ifFalse);
            }
            case ExpressionType.New:
            {
                NewExpression @new = (NewExpression)expression;
                if (@new.Members == null)
                    return New(@new.Constructor, @new.Arguments.Select(Visit));

                return New(@new.Constructor, @new.Arguments.Select(Visit), @new.Members);
            }
            case ExpressionType.NewArrayBounds:
            {
                NewArrayExpression newArrayBounds = (NewArrayExpression)expression;
                Type? elementType = newArrayBounds.Type.GetElementType();
                if (elementType == null)
                    return expression;
                return NewArrayBounds(elementType, newArrayBounds.Expressions.Select(Visit));
            }
            case ExpressionType.NewArrayInit:
            {
                NewArrayExpression newArrayInit = (NewArrayExpression)expression;
                Type? elementType = newArrayInit.Type.GetElementType();
                if (elementType == null)
                    return expression;
                return NewArrayInit(elementType, newArrayInit.Expressions.Select(Visit));
            }
            case ExpressionType.Invoke:
            {
                InvocationExpression invocation = (InvocationExpression)expression;
                return Invoke(Visit(invocation.Expression), invocation.Arguments.Select(Visit));
            }
            case ExpressionType.MemberInit:
            {
                MemberInitExpression memberInit = (MemberInitExpression)expression;
                NewExpression visitedNew = (NewExpression)Visit(memberInit.NewExpression);
                int count = memberInit.Bindings.Count;
                MemberBinding[] visitedBindings = new MemberBinding[count];
                bool changed = visitedNew != memberInit.NewExpression;
                for (int i = 0; i < count; i++)
                {
                    MemberBinding binding = memberInit.Bindings[i];
                    MemberBinding visited = VisitBinding(binding);
                    visitedBindings[i] = visited;
                    if (!ReferenceEquals(visited, binding))
                        changed = true;
                }

                if (!changed)
                    return memberInit;

                return MemberInit(visitedNew, visitedBindings);
            }
            case ExpressionType.ListInit:
            {
                ListInitExpression listInit = (ListInitExpression)expression;
                NewExpression visitedNew = (NewExpression)Visit(listInit.NewExpression);
                int count = listInit.Initializers.Count;
                ElementInit[] visitedInitializers = new ElementInit[count];
                bool changed = visitedNew != listInit.NewExpression;
                for (int i = 0; i < count; i++)
                {
                    ElementInit initializer = listInit.Initializers[i];
                    ElementInit visited = VisitElementInit(initializer);
                    visitedInitializers[i] = visited;
                    if (!ReferenceEquals(visited, initializer))
                        changed = true;
                }

                if (!changed)
                    return listInit;

                return ListInit(visitedNew, visitedInitializers);
            }
        }

        if (expression is UnaryExpression unary)
        {
            Expression visited = Visit(unary.Operand);
            if (visited == unary.Operand)
                return unary;

            return MakeUnary(unary.NodeType, visited, unary.Type, unary.Method);
        }

        if (expression is BinaryExpression binary)
        {
            Expression left = Visit(binary.Left);
            Expression right = Visit(binary.Right);
            if (left == binary.Left && right == binary.Right)
                return binary;

            return MakeBinary(expression.NodeType, left, right, binary.IsLiftedToNull, binary.Method);
        }

        if (expression.NodeType == ExpressionType.Extension)
            return expression;

        throw new InvalidOperationException("Unknown expression: " + expression.NodeType);
    }
}