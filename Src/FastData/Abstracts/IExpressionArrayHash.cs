using System.Linq.Expressions;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Abstracts;

public interface IExpressionArrayHash : IArrayHash
{
    Expression<ArrayHashFunc> BuildExpression();
}