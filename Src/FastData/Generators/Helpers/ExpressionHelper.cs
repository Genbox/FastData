using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Generators.Helpers;

public static class ExpressionHelper
{
    internal static string Print(Mixer mixer) => mixer(Variable(typeof(ulong), "hash"), Variable(typeof(ulong), "Value")).ToString();

    internal static string Print(Avalanche avalanche) => avalanche(Variable(typeof(ulong), "hash")).ToString();

    public static string Print(Expression exp)
    {
        PropertyInfo? propertyInfo = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);

        if (propertyInfo == null)
            throw new InvalidOperationException("Unable to get DebugView property");

        return (string)propertyInfo.GetValue(exp)!;
    }

    public static IEnumerable<AnnotatedExpr> Transform(ICollection<AnnotatedExpr> expressions, ICollection<IExprTransform> transforms)
    {
        if (transforms.Count == 0)
            return expressions;

        // Apply transforms sequentially so each stage sees the previous output.
        ICollection<AnnotatedExpr> current = expressions;

        foreach (IExprTransform trans in transforms)
        {
            object state = trans.CreateState();
            List<AnnotatedExpr> next = new List<AnnotatedExpr>(current.Count + 8);

            foreach (AnnotatedExpr expr in current)
                trans.Transform(expr, state, next);

            current = next;
        }

        return current;
    }
}