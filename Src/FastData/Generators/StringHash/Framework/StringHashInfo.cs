using System.Linq.Expressions;

namespace Genbox.FastData.Generators.StringHash.Framework;

/// <summary>Describes a string hash expression and any arrays it captures.</summary>
/// <param name="expression">The hash expression.</param>
/// <param name="additionalData">Metadata for captured arrays.</param>
public sealed class StringHashInfo(Expression expression, AdditionalData[]? additionalData)
{
    /// <summary>Gets the hash expression.</summary>
    public Expression Expression { get; } = expression;

    /// <summary>Gets metadata for arrays captured by the expression.</summary>
    public AdditionalData[]? AdditionalData { get; } = additionalData;
}