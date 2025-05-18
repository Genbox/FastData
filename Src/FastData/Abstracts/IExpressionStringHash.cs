using System.Linq.Expressions;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Abstracts;

public interface IExpressionStringHash : IStringHash
{
    Expression<HashFunc> BuildExpression();
}