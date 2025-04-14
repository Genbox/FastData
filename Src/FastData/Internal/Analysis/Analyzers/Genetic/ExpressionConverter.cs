using System.Linq.Expressions;
using System.Text;

namespace Genbox.FastData.Internal.Analysis.Techniques.Genetic;

internal sealed class ExpressionConverter : ExpressionVisitor
{
    private readonly StringBuilder _sb = new StringBuilder();

    private ExpressionConverter() {}
    internal static ExpressionConverter Instance => new ExpressionConverter();

    public string GetCode(Expression expression)
    {
        _sb.Clear();
        Visit(expression);
        return _sb.ToString();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.Assign)
        {
            Visit(node.Left);
            _sb.Append(" = ");
            Visit(node.Right);
        }
        else
        {
            _sb.Append('(');
            Visit(node.Left);
            _sb.Append(GetBinaryOperator(node.NodeType));
            Visit(node.Right);
            _sb.Append(')');
        }

        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        _sb.Append(node.Value);
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        _sb.Append(node.Name);
        return node;
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        foreach (Expression? expression in node.Expressions)
        {
            Visit(expression);
            _sb.AppendLine(";");
        }
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        Visit(node.Body);
        return node;
    }

    private static string GetBinaryOperator(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.Add => " + ",
            ExpressionType.Multiply => " * ",
            ExpressionType.ExclusiveOr => " ^ ",
            ExpressionType.LeftShift => " << ",
            ExpressionType.RightShift => " >> ",
            ExpressionType.Or => " | ",
            _ => throw new NotSupportedException($"Operator {type} is not supported.")
        };
    }
}