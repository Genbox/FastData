using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.Abstracts;

public interface IExprTransform
{
    object CreateState();
    IEnumerable<AnnotatedExpr> Transform(AnnotatedExpr expr, object state);
}