using System.Linq.Expressions;
using System.Reflection;
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
            AllocationSignature signature = AllocationSignature.Create(call);

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
        public HashSet<AllocationSignature> Seen { get; } = new HashSet<AllocationSignature>();
    }

    private readonly struct AllocationSignature(MethodInfo method, ArgumentSignature[] arguments) : IEquatable<AllocationSignature>
    {
        private MethodInfo Method { get; } = method;
        private ArgumentSignature[] Arguments { get; } = arguments;

        public static AllocationSignature Create(MethodCallExpression node)
        {
            ArgumentSignature[] arguments = new ArgumentSignature[node.Arguments.Count];
            for (int i = 0; i < node.Arguments.Count; i++)
                arguments[i] = ArgumentSignature.Create(node.Arguments[i]);

            return new AllocationSignature(node.Method, arguments);
        }

        public bool Equals(AllocationSignature other)
        {
            if (!Equals(Method, other.Method) || Arguments.Length != other.Arguments.Length)
                return false;

            for (int i = 0; i < Arguments.Length; i++)
            {
                if (!Arguments[i].Equals(other.Arguments[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is AllocationSignature other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Method);
            foreach (ArgumentSignature argument in Arguments)
                hash.Add(argument);

            return hash.ToHashCode();
        }
    }

    private readonly struct ArgumentSignature : IEquatable<ArgumentSignature>
    {
        private ArgumentSignature(ArgumentKind kind, Type type, object? value, string? name, string? text)
        {
            Kind = kind;
            Type = type;
            Value = value;
            Name = name;
            Text = text;
        }

        private ArgumentKind Kind { get; }
        private Type Type { get; }
        private object? Value { get; }
        private string? Name { get; }
        private string? Text { get; }

        public static ArgumentSignature Create(Expression expression)
        {
            if (expression is ConstantExpression constant)
                return new ArgumentSignature(ArgumentKind.Constant, constant.Type, constant.Value, null, null);

            if (expression is ParameterExpression parameter)
                return new ArgumentSignature(ArgumentKind.Parameter, parameter.Type, null, parameter.Name, null);

            return new ArgumentSignature(ArgumentKind.Other, expression.Type, null, null, expression.ToString());
        }

        public bool Equals(ArgumentSignature other)
        {
            if (Kind != other.Kind || Type != other.Type)
                return false;

            return Kind switch
            {
                ArgumentKind.Constant => Equals(Value, other.Value),
                ArgumentKind.Parameter => string.Equals(Name, other.Name, StringComparison.Ordinal),
                ArgumentKind.Other => string.Equals(Text, other.Text, StringComparison.Ordinal),
                _ => false
            };
        }

        public override bool Equals(object? obj) => obj is ArgumentSignature other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Kind);
            hash.Add(Type);
            hash.Add(Value);
            hash.Add(Name, StringComparer.Ordinal);
            hash.Add(Text, StringComparer.Ordinal);
            return hash.ToHashCode();
        }
    }

    private enum ArgumentKind
    {
        Constant,
        Parameter,
        Other
    }
}