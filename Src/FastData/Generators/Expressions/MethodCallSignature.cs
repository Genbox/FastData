using System.Linq.Expressions;
using System.Reflection;

namespace Genbox.FastData.Generators.Expressions;

internal readonly struct MethodCallSignature(MethodInfo method, ArgumentSignature[] arguments) : IEquatable<MethodCallSignature>
{
    private MethodInfo Method { get; } = method;
    private ArgumentSignature[] Arguments { get; } = arguments;

    public static MethodCallSignature Create(MethodCallExpression node)
    {
        ArgumentSignature[] args = new ArgumentSignature[node.Arguments.Count];
        for (int i = 0; i < node.Arguments.Count; i++)
            args[i] = ArgumentSignature.Create(node.Arguments[i]);

        return new MethodCallSignature(node.Method, args);
    }

    public bool Equals(MethodCallSignature other)
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

    public override bool Equals(object? obj) => obj is MethodCallSignature other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Method);
        foreach (ArgumentSignature arg in Arguments)
            hash.Add(arg);

        return hash.ToHashCode();
    }
}