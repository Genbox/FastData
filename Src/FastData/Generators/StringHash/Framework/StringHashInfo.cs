using System.Linq.Expressions;

namespace Genbox.FastData.Generators.StringHash.Framework;

public sealed class StringHashInfo(Expression expression, AdditionalData[]? additionalData)
{
    public Expression Expression { get; } = expression;
    public AdditionalData[]? AdditionalData { get; } = additionalData;
}