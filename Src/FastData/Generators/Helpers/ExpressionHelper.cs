using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Generators.Helpers;

/// <summary>Provides diagnostic printing and transformation helpers for generator expressions.</summary>
public static class ExpressionHelper
{
    internal static string Print(Mixer mixer) => mixer(Variable(typeof(ulong), "hash"), Variable(typeof(ulong), "Value")).ToString();

    internal static string Print(Avalanche avalanche) => avalanche(Variable(typeof(ulong), "hash")).ToString();

    /// <summary>Formats an expression using the runtime diagnostic representation.</summary>
    /// <param name="exp">The expression to format.</param>
    /// <returns>The diagnostic expression text.</returns>
    [SuppressMessage("Security", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Expression.DebugView is intentionally used for diagnostic output only.")]
    public static string Print(Expression exp)
    {
        if (exp == null)
            throw new ArgumentNullException(nameof(exp));

        PropertyInfo? propertyInfo = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);

        if (propertyInfo == null)
            throw new InvalidOperationException("Unable to get DebugView property");

        return (string)propertyInfo.GetValue(exp)!;
    }

    /// <summary>Applies expression transformations in sequence.</summary>
    /// <param name="expressions">The input expressions.</param>
    /// <param name="transforms">The transformations to apply.</param>
    /// <returns>The transformed expressions.</returns>
    public static IEnumerable<AnnotatedExpr> Transform(ICollection<AnnotatedExpr> expressions, ICollection<IExprTransform> transforms)
    {
        if (expressions == null)
            throw new ArgumentNullException(nameof(expressions));
        if (transforms == null)
            throw new ArgumentNullException(nameof(transforms));

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