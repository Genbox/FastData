using System.Linq.Expressions;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStringHash
{
    HashFunc<string> GetHashFunction();
    Expression<HashFunc<string>> GetExpression();
}