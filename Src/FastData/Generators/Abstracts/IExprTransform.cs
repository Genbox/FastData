using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.Abstracts;

public interface IExprTransform
{
    object CreateState();
    void Transform(AnnotatedExpr expr, object state, List<AnnotatedExpr> output);
}