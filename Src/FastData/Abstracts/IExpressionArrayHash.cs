using System.Linq.Expressions;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Abstracts;

public interface IExpressionArrayHash : IArrayHash
{
    Expression<HashFunc> BuildExpression();
}