using System.Linq.Expressions;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Abstracts;

public interface IStringHash
{
    HashFunc<string> GetHashFunction();
    Expression<HashFunc<string>> GetExpression();
}