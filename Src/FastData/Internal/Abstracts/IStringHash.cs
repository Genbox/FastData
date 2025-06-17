using System.Linq.Expressions;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStringHash
{
    HashFunc<string> GetHashFunction();
    Expression<HashFunc<string>> GetExpression();
    ReaderFunctions Functions { get; }
    State[]? State { get; }
}