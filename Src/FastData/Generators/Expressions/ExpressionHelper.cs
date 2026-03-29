using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Expressions;

public static class ExpressionHelper
{
    public static IEnumerable<AnnotatedExpr> Transform(ICollection<AnnotatedExpr> expressions, ICollection<IExprTransform> transforms)
    {
        if (transforms.Count == 0)
        {
            foreach (AnnotatedExpr expression in expressions)
                yield return expression;

            yield break;
        }

        // Apply transforms sequentially so each stage sees the previous output.
        IEnumerable<AnnotatedExpr> current = expressions;

        foreach (IExprTransform trans in transforms)
        {
            object state = trans.CreateState();
            List<AnnotatedExpr> next = new List<AnnotatedExpr>();

            foreach (AnnotatedExpr expr in current)
            {
                foreach (AnnotatedExpr newExpr in trans.Transform(expr, state))
                    next.Add(newExpr);
            }

            current = next;
        }

        foreach (AnnotatedExpr expr in current)
            yield return expr;
    }
}