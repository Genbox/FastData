using System.Linq.Expressions;

namespace Genbox.FastData.Generators.StringHash.Framework;

public sealed class NumericHashInfo(Expression expression)
{
    public Expression Expression { get; } = expression;
}