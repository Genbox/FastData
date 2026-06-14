using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Removes duplicate allocation assignments produced by earlier expression transforms.</summary>
/// <remarks>
/// Mandatory expressions can intentionally allocate values such as <c>length = Length(key)</c>. The allocation gatherer can
/// discover the same call later while transforming early exits, so this transform keeps the earliest allocation and drops
/// later equivalent method-call assignments.
/// </remarks>
public sealed class DeduplicateAllocationTransform : IExprTransform
{
    public object CreateState() => new DeduplicateAllocationState();

    public IEnumerable<AnnotatedExpr> Transform(AnnotatedExpr expr, object state)
    {
        if (expr.Kind != ExprKind.Assignment || expr.Expression is not BinaryExpression { NodeType: ExpressionType.Assign } assignment)
        {
            yield return expr;
            yield break;
        }

        if (IsSelfAssignment(assignment))
            yield break;

        // Only method-call allocations are deduplicated. Other assignments can have side effects or carry values that are
        // not safely comparable by the call signature rules below.
        if (assignment.Right is MethodCallExpression call)
        {
            DeduplicateAllocationState dedupeState = (DeduplicateAllocationState)state;
            MethodCallSignature signature = MethodCallSignature.Create(call);

            if (!dedupeState.Seen.Add(signature))
                yield break;
        }

        yield return expr;
    }

    private static bool IsSelfAssignment(BinaryExpression assignment)
    {
        if (assignment.Left is not ParameterExpression left || assignment.Right is not ParameterExpression right)
            return false;

        return left.Type == right.Type && string.Equals(left.Name, right.Name, StringComparison.Ordinal);
    }

    private sealed class DeduplicateAllocationState
    {
        public HashSet<MethodCallSignature> Seen { get; } = new HashSet<MethodCallSignature>();
    }
}