using System.Linq.Expressions;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus;

public sealed class CPlusPlusExpressionCompiler(TypeMap map) : ExpressionCompiler(map)
{
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null && node.Member.Name == nameof(string.Length) && node.Expression.Type == typeof(string))
        {
            Visit(node.Expression);
            Output.Append(".length()");
            return node;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(string))
        {
            if (node.Method.Name == nameof(string.StartsWith))
                return RenderStringCompare(node, true);
            if (node.Method.Name == nameof(string.EndsWith))
                return RenderStringCompare(node, false);
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert)
        {
            Output.Append("static_cast<").Append(map.GetTypeName(node.Type)).Append(">(");
            Visit(node.Operand);
            Output.Append(")");
            return node;
        }

        return base.VisitUnary(node);
    }

    private Expression RenderStringCompare(MethodCallExpression node, bool isPrefix)
    {
        if (node.Object != null && node.Arguments.Count > 0 && node.Arguments[0] is ConstantExpression constExpr && constExpr.Value is string literal)
        {
            int length = literal.Length;

            Visit(node.Object);
            if (isPrefix)
            {
                Output.Append(".compare(0, ")
                      .Append(length)
                      .Append(", ")
                      .Append(map.ToValueLabel(literal))
                      .Append(") == 0");
            }
            else
            {
                Output.Append(".compare(");
                Visit(node.Object);
                Output.Append(".length() - ")
                      .Append(length)
                      .Append(", ")
                      .Append(length)
                      .Append(", ")
                      .Append(map.ToValueLabel(literal))
                      .Append(") == 0");
            }

            return node;
        }

        return base.VisitMethodCall(node);
    }
}