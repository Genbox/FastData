using System.Linq;
using System.Linq.Expressions;

namespace Genbox.FastData.Generator.CSharp;

public sealed class CSharpExpressionCompiler(TypeMap map) : ExpressionCompiler(map)
{
    protected override Expression VisitBlock(BlockExpression node)
    {
        if (node.Expressions.Count == 0 ||
            node.Expressions[node.Expressions.Count - 1] is not ParameterExpression result ||
            !node.Variables.Any(variable => ReferenceEquals(variable, result)))
        {
            return base.VisitBlock(node);
        }

        // The hash template returns the block's result local explicitly; it is not a valid standalone C# statement.
        BlockExpression statements = Expression.Block(node.Variables, node.Expressions.Take(node.Expressions.Count - 1));
        base.VisitBlock(statements);
        return node;
    }
}