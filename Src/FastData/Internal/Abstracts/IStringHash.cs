using System.Linq.Expressions;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStringHash
{
    ReaderFunctions Functions { get; }
    AdditionalData[]? AdditionalData { get; }
    Expression<StringHashFunc> GetExpression();
}