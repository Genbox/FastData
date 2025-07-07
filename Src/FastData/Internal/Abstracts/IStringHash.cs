using System.Linq.Expressions;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStringHash
{
    Expression<StringHashFunc> GetExpression();
    ReaderFunctions Functions { get; }
    State[]? State { get; }
}