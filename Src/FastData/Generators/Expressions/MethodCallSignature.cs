using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Expressions;

internal readonly struct MethodCallSignature(MethodCallExpression node) : IEquatable<MethodCallSignature>
{
    private MethodCallExpression Node { get; } = node;

    public static MethodCallSignature Create(MethodCallExpression node) => new MethodCallSignature(node);

    public bool Equals(MethodCallSignature other)
    {
        if (!Equals(Node.Method, other.Node.Method) || Node.Arguments.Count != other.Node.Arguments.Count)
            return false;

        for (int i = 0; i < Node.Arguments.Count; i++)
        {
            if (!ArgumentSignature.Equals(Node.Arguments[i], other.Node.Arguments[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is MethodCallSignature other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Node.Method);

        for (int i = 0; i < Node.Arguments.Count; i++)
            ArgumentSignature.AddHashCode(ref hash, Node.Arguments[i]);

        return hash.ToHashCode();
    }
}