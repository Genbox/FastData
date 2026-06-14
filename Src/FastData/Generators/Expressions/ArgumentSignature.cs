using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Expressions;

internal readonly struct ArgumentSignature : IEquatable<ArgumentSignature>
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