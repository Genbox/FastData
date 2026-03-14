using System.Linq.Expressions;

namespace Genbox.FastData.Generators.StringHash.Framework;

public sealed class StringHashInfo(Expression expression, ReaderFunctions functions, AdditionalData[]? additionalData)
{
    public Expression Expression { get; } = expression;
    public ReaderFunctions Functions { get; } = functions;
    public AdditionalData[]? AdditionalData { get; } = additionalData;
}