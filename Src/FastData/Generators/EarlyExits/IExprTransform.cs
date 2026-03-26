namespace Genbox.FastData.Generators.EarlyExits;

public interface IExprTransform
{
    object CreateState();
    IEnumerable<AnnotatedExpr> Transform(AnnotatedExpr expr, object state);
}