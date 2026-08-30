using System.Linq.Expressions;

namespace Genbox.FastData.Generators.StringHash.Framework;

/// <summary>Describes a numeric hash expression.</summary>
/// <param name="expression">The hash expression.</param>
public sealed class NumericHashInfo(Expression expression)
{
    /// <summary>Gets the hash expression.</summary>
    public Expression Expression { get; } = expression;
}