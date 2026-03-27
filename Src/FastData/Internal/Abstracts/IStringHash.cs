using System.Linq.Expressions;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStringHash
{
    AdditionalData[]? AdditionalData { get; }
    Expression<StringHashFunc> GetExpression();
}