using System.Linq.Expressions;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal sealed class CPlusPlusEarlyExitDef(ExpressionCompiler compiler) : IEarlyExitDef
{
    public string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits, MethodType methodType, string keyName)
    {
        IEarlyExit[] earlyExitArray = earlyExits as IEarlyExit[] ?? earlyExits.ToArray();

        if (earlyExitArray.Length == 0)
            return string.Empty;

        ParameterExpression parameter = Expression.Parameter(typeof(T), keyName);
        List<AnnotatedExpr> annotated = new List<AnnotatedExpr>(earlyExitArray.Length);

        foreach (IEarlyExit earlyExit in earlyExitArray)
            annotated.Add(AnnotatedExpr.EarlyExit(earlyExit.GetExpression(parameter)));

        IExprTransform[] transforms =
        [
            new EarlyExitConditionTransform(GetBody(methodType)),
            new AllocationGatherTransform()
        ];

        IEnumerable<AnnotatedExpr> transformed = EarlyExitManager.Transform(annotated, transforms);
        BlockExpression block = ToBlock(transformed);
        return block.Expressions.Count == 0 ? string.Empty : compiler.GetCode(block, 4);
    }

    private static IReadOnlyList<Expression> GetBody(MethodType methodType) => methodType == MethodType.TryLookup
        ?
        [
            Expression.Assign(Symbol("value"), Symbol("nullptr")),
            ReturnSymbol("false")
        ]
        : [ReturnSymbol("false")];

    private static ParameterExpression Symbol(string name) => Expression.Parameter(typeof(object), name);

    private static GotoExpression ReturnSymbol(string symbol)
    {
        ParameterExpression value = Symbol(symbol);
        LabelTarget target = Expression.Label(value.Type);
        return Expression.MakeGoto(GotoExpressionKind.Break, target, value, value.Type);
    }

    private static BlockExpression ToBlock(IEnumerable<AnnotatedExpr> expressions)
    {
        List<ParameterExpression> variables = new List<ParameterExpression>();
        List<Expression> statements = new List<Expression>();

        foreach (AnnotatedExpr expr in expressions)
        {
            if (expr.Kind == ExprKind.Assignment && expr.Expression is BinaryExpression { NodeType: ExpressionType.Assign, Left: ParameterExpression left })
                variables.Add(left);

            statements.Add(expr.Expression);
        }

        return Expression.Block(variables, statements);
    }
}