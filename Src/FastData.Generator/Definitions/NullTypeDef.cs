using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Describes the target-language representation of a null value.</summary>
/// <param name="nullLabel">The target-language null literal.</param>
public class NullTypeDef(string nullLabel) : ITypeDef
{
    /// <inheritdoc />
    public TypeCode KeyType => TypeCode.Empty;

    /// <inheritdoc />
    public string Name => throw new InvalidOperationException("Null does not have a name");

    /// <inheritdoc />
    public Func<TypeMap, object, string> PrintObj { get; } = (_, _) => nullLabel;
}