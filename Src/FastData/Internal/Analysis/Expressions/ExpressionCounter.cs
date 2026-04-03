using System.Linq.Expressions;

namespace Genbox.FastData.Internal.Analysis.Expressions;

internal sealed class ExpressionCounter : ExpressionVisitor
{
    public int Count { get; private set; }

    internal static int CountNodes(Expression expression)
    {
        ExpressionCounter counter = new ExpressionCounter();
        counter.Visit(expression);
        return counter.Count;
    }

    public override Expression? Visit(Expression? node)
    {
        Count++;
        return base.Visit(node);
    }
}