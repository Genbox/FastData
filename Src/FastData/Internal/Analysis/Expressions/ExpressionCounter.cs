using System.Linq.Expressions;

namespace Genbox.FastData.Internal.Analysis.Expressions;

internal sealed class ExpressionCounter : ExpressionVisitor
{
    public int Count { get; private set; }

    public override Expression? Visit(Expression? node)
    {
        Count++;
        return base.Visit(node);
    }
}