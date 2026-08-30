using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Removes identity assignments produced by earlier expression transforms.</summary>
/// <remarks>
/// Mandatory expressions can intentionally allocate values such as <c>length = Length(key)</c>. The allocation gatherer can
/// discover the same call later while transforming early exits. It rewrites that repeated allocation to an identity assignment,
/// which this transform removes without assuming that symbols or helper inputs are immutable.
/// </remarks>
public sealed class DeduplicateAllocationTransform : IExprTransform
{
    /// <inheritdoc />
    public object CreateState() => new object();

    /// <inheritdoc />
    public void Transform(AnnotatedExpr expr, object state, ICollection<AnnotatedExpr> output)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        if (expr.Kind != ExprKind.Assignment || expr.Expression is not BinaryExpression { NodeType: ExpressionType.Assign } assignment)
        {
            output.Add(expr);
            return;
        }

        if (assignment.Left is ParameterExpression left &&
            assignment.Right is ParameterExpression right &&
            ReferenceEquals(left, right))
            return;

        output.Add(expr);
    }
}