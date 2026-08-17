using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Tests;

public class DeduplicateAllocationTransformTests
{
    [Fact]
    public void Transform_DropsDuplicateMethodCallAssignments()
    {
        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
        AnnotatedExpr[] expressions =
        [
            AnnotatedExpr.Allocation(Expression.Assign(Expression.Variable(typeof(int), "length"), Expression.Call(methodInfo, key))),
            AnnotatedExpr.Allocation(Expression.Assign(Expression.Variable(typeof(int), "length"), Expression.Call(methodInfo, key)))
        ];

        DeduplicateAllocationTransform transform = new DeduplicateAllocationTransform();
        object state = transform.CreateState();
        List<AnnotatedExpr> transformed = new List<AnnotatedExpr>();

        foreach (AnnotatedExpr expression in expressions)
            transform.Transform(expression, state, transformed);

        Assert.Single(transformed);
        Assert.Same(expressions[0].Expression, transformed[0].Expression);
    }

    [Fact]
    public void Transform_DropsSelfAssignment()
    {
        ParameterExpression length = Expression.Variable(typeof(int), "length");
        AnnotatedExpr expression = AnnotatedExpr.Allocation(Expression.Assign(length, Expression.Variable(typeof(int), "length")));

        DeduplicateAllocationTransform transform = new DeduplicateAllocationTransform();
        List<AnnotatedExpr> transformed = new List<AnnotatedExpr>();
        transform.Transform(expression, transform.CreateState(), transformed);

        Assert.Empty(transformed);
    }
}