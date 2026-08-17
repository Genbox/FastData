using System.Linq.Expressions;
using BenchmarkDotNet.Order;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.Expressions.Optimizer;
using Genbox.FastData.Generators.Helpers;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EarlyExitPipelineBenchmarks
{
    private AnnotatedExpr[] _expressions = null!;
    private IExprTransform[] _transforms = null!;

    [Params(8, 32, 128)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        ParameterExpression key = Expression.Variable(typeof(string), "key");
        _expressions = new AnnotatedExpr[Count];

        for (int i = 0; i < _expressions.Length; i++)
        {
            MethodCallExpression length = Expression.Call(typeof(GeneratorFunctions), nameof(GeneratorFunctions.Length), null, key);
            MethodCallExpression unit = Expression.Call(typeof(GeneratorFunctions), nameof(GeneratorFunctions.UnitAt), null, key, Expression.Constant(i & 3));
            BinaryExpression lengthCheck = Expression.LessThan(length, Expression.Constant(4 + (i & 7)));
            BinaryExpression unitCheck = Expression.NotEqual(unit, Expression.Constant((uint)('a' + (i % 26))));
            _expressions[i] = AnnotatedExpr.EarlyExit(Expression.OrElse(lengthCheck, unitCheck));
        }

        _transforms = [new AllocationGatherTransform(), new DeduplicateAllocationTransform()];

        if (ExpressionHelper.Transform(_expressions, _transforms).Count() != Count + 5)
            throw new InvalidOperationException("Unexpected early-exit transform output count.");
    }

    [Benchmark]
    public int Transform() => ExpressionHelper.Transform(_expressions, _transforms).Count();

    [Benchmark]
    public int OptimizeExpressions()
    {
        int optimized = 0;

        foreach (AnnotatedExpr expression in _expressions)
            optimized += (int)ExprOptimizer.Visit(expression.Expression).NodeType;

        return optimized;
    }
}