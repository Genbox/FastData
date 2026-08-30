using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Describes the target-language representation of Boolean values.</summary>
/// <param name="name">The target-language Boolean type name.</param>
public sealed class BooleanTypeDef(string name) : ITypeDef<bool>
{
    /// <inheritdoc />
    public TypeCode KeyType => TypeCode.Boolean;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public Func<TypeMap, object, string> PrintObj { get; } = static (_, value) => (bool)value ? "true" : "false";

    /// <inheritdoc />
    public Func<TypeMap, bool, string> Print { get; } = static (_, value) => value ? "true" : "false";
}