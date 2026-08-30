using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.Helpers;

namespace Genbox.FastData.Tests;

public class DeduplicateAllocationTransformTests
{
    [Fact]
    public void Transform_KeepsRepeatedMethodCallAssignmentsWithoutGathering()
    {
        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        ParameterExpression length = Expression.Variable(typeof(int), "length");
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        AnnotatedExpr[] expressions =
        [
            AnnotatedExpr.Allocation(Expression.Assign(length, Expression.Call(methodInfo, key))),
            AnnotatedExpr.Allocation(Expression.Assign(length, Expression.Call(methodInfo, key)))
        ];

        DeduplicateAllocationTransform transform = new DeduplicateAllocationTransform();
        object state = transform.CreateState();
        List<AnnotatedExpr> transformed = new List<AnnotatedExpr>();

        foreach (AnnotatedExpr expression in expressions)
            transform.Transform(expression, state, transformed);

        Assert.Equal(2, transformed.Count);
    }

    [Fact]
    public void Transform_KeepsDuplicateMethodCallAssignmentsToDistinctSymbols()
    {
        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        AnnotatedExpr[] expressions =
        [
            AnnotatedExpr.Allocation(Expression.Assign(Expression.Variable(typeof(int), "firstLength"), Expression.Call(methodInfo, key))),
            AnnotatedExpr.Allocation(Expression.Assign(Expression.Variable(typeof(int), "secondLength"), Expression.Call(methodInfo, key)))
        ];

        DeduplicateAllocationTransform transform = new DeduplicateAllocationTransform();
        object state = transform.CreateState();
        List<AnnotatedExpr> transformed = new List<AnnotatedExpr>();

        foreach (AnnotatedExpr expression in expressions)
            transform.Transform(expression, state, transformed);

        Assert.Equal(2, transformed.Count);
    }

    [Fact]
    public void Transform_DropsIdentitySelfAssignment()
    {
        ParameterExpression length = Expression.Variable(typeof(int), "length");
        AnnotatedExpr expression = AnnotatedExpr.Allocation(Expression.Assign(length, length));

        DeduplicateAllocationTransform transform = new DeduplicateAllocationTransform();
        List<AnnotatedExpr> transformed = new List<AnnotatedExpr>();
        transform.Transform(expression, transform.CreateState(), transformed);

        Assert.Empty(transformed);
    }

    [Fact]
    public void Transform_KeepsAssignmentBetweenDistinctSameNameSymbols()
    {
        ParameterExpression left = Expression.Variable(typeof(int), "length");
        ParameterExpression right = Expression.Variable(typeof(int), "length");
        AnnotatedExpr expression = AnnotatedExpr.Allocation(Expression.Assign(left, right));

        DeduplicateAllocationTransform transform = new DeduplicateAllocationTransform();
        List<AnnotatedExpr> transformed = new List<AnnotatedExpr>();
        transform.Transform(expression, transform.CreateState(), transformed);

        Assert.Single(transformed);
    }

    [Fact]
    public void AllocationGather_ReusesExistingAllocationSymbol()
    {
        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        ParameterExpression length = Expression.Variable(typeof(int), "length");
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        AnnotatedExpr allocation = AnnotatedExpr.Allocation(Expression.Assign(length, Expression.Call(methodInfo, key)));
        AnnotatedExpr condition = AnnotatedExpr.EarlyExit(Expression.LessThan(Expression.Call(methodInfo, key), Expression.Constant(4)));
        AllocationGatherTransform transform = new AllocationGatherTransform();
        object state = transform.CreateState();
        List<AnnotatedExpr> transformed = new List<AnnotatedExpr>();

        transform.Transform(allocation, state, transformed);
        transform.Transform(condition, state, transformed);

        Assert.Equal(2, transformed.Count);
        BinaryExpression updatedCondition = Assert.IsAssignableFrom<BinaryExpression>(transformed[1].Expression);
        Assert.Same(length, updatedCondition.Left);
    }

    [Fact]
    public void AllocationGather_InvalidatesCachedResultWhenResultSymbolIsWritten()
    {
        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        ParameterExpression length = Expression.Variable(typeof(int), "length");
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        AnnotatedExpr[] expressions =
        [
            AnnotatedExpr.Allocation(Expression.Assign(length, Expression.Call(methodInfo, key))),
            AnnotatedExpr.Allocation(Expression.Assign(length, Expression.Constant(0))),
            AnnotatedExpr.EarlyExit(Expression.LessThan(Expression.Call(methodInfo, key), Expression.Constant(4)))
        ];

        AnnotatedExpr[] transformed = ExpressionHelper
            .Transform(expressions, [new AllocationGatherTransform(), new DeduplicateAllocationTransform()])
            .ToArray();

        Assert.Equal(4, transformed.Length);
        BinaryExpression refreshedAllocation = Assert.IsAssignableFrom<BinaryExpression>(transformed[2].Expression);
        Assert.IsAssignableFrom<MethodCallExpression>(refreshedAllocation.Right);
        Assert.NotEqual(length.Name, Assert.IsAssignableFrom<ParameterExpression>(refreshedAllocation.Left).Name, StringComparer.Ordinal);
        BinaryExpression updatedCondition = Assert.IsAssignableFrom<BinaryExpression>(transformed[3].Expression);
        Assert.Same(refreshedAllocation.Left, updatedCondition.Left);
    }

    [Fact]
    public void AllocationGather_InvalidatesCachedResultWhenInputSymbolIsWritten()
    {
        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        AnnotatedExpr[] expressions =
        [
            AnnotatedExpr.EarlyExit(Expression.LessThan(Expression.Call(methodInfo, key), Expression.Constant(4))),
            AnnotatedExpr.Allocation(Expression.Assign(key, Expression.Constant("updated"))),
            AnnotatedExpr.EarlyExit(Expression.GreaterThan(Expression.Call(methodInfo, key), Expression.Constant(8)))
        ];

        AnnotatedExpr[] transformed = ExpressionHelper
            .Transform(expressions, [new AllocationGatherTransform(), new DeduplicateAllocationTransform()])
            .ToArray();

        Assert.Equal(5, transformed.Length);
        BinaryExpression firstCondition = Assert.IsAssignableFrom<BinaryExpression>(transformed[1].Expression);
        BinaryExpression refreshedAllocation = Assert.IsAssignableFrom<BinaryExpression>(transformed[3].Expression);
        BinaryExpression secondCondition = Assert.IsAssignableFrom<BinaryExpression>(transformed[4].Expression);
        Assert.NotSame(firstCondition.Left, refreshedAllocation.Left);
        Assert.NotEqual(
            Assert.IsAssignableFrom<ParameterExpression>(firstCondition.Left).Name,
            Assert.IsAssignableFrom<ParameterExpression>(refreshedAllocation.Left).Name,
            StringComparer.Ordinal);
        Assert.Same(refreshedAllocation.Left, secondCondition.Left);
    }

    [Fact]
    public void AllocationGather_DistinguishesSameNameInputSymbols()
    {
        ParameterExpression firstKey = Expression.Parameter(typeof(string), "key");
        ParameterExpression secondKey = Expression.Parameter(typeof(string), "key");
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        AnnotatedExpr[] expressions =
        [
            AnnotatedExpr.EarlyExit(Expression.LessThan(Expression.Call(methodInfo, firstKey), Expression.Constant(4))),
            AnnotatedExpr.EarlyExit(Expression.GreaterThan(Expression.Call(methodInfo, secondKey), Expression.Constant(8)))
        ];

        AnnotatedExpr[] transformed = ExpressionHelper.Transform(expressions, [new AllocationGatherTransform()]).ToArray();

        Assert.Equal(4, transformed.Length);
        BinaryExpression firstAssignment = Assert.IsAssignableFrom<BinaryExpression>(transformed[0].Expression);
        BinaryExpression secondAssignment = Assert.IsAssignableFrom<BinaryExpression>(transformed[2].Expression);
        ParameterExpression firstAllocation = Assert.IsAssignableFrom<ParameterExpression>(firstAssignment.Left);
        ParameterExpression secondAllocation = Assert.IsAssignableFrom<ParameterExpression>(secondAssignment.Left);
        Assert.NotSame(firstAllocation, secondAllocation);
        Assert.NotEqual(firstAllocation.Name, secondAllocation.Name, StringComparer.Ordinal);
    }
}