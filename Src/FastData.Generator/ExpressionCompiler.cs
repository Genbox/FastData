using System.Linq.Expressions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator;

public abstract class ExpressionCompiler(TypeHelper helper) : ExpressionVisitor
{
    protected readonly FastStringBuilder Sb = new FastStringBuilder();

    public string GetCode(Expression expression)
    {
        Sb.Clear();
        Visit(expression);
        return Sb.ToString();
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            Visit(node.Object);
            Sb.Append(".");
        }
        Sb.Append(node.Method.Name).Append("(");
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Visit(node.Arguments[i]);
            if (i < node.Arguments.Count - 1) Sb.Append(", ");
        }
        Sb.Append(")");
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        Visit(node.Body);
        return node;
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        foreach (ParameterExpression v in node.Variables)
            Sb.AppendLine($"{helper.GetTypeName(v.Type)} {v.Name};");

        foreach (Expression expr in node.Expressions)
        {
            Visit(expr);
            if (expr is LoopExpression)
                Sb.AppendLine();
            else
                Sb.AppendLine(";");
        }
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        bool isAssign = node.NodeType is ExpressionType.Assign or ExpressionType.AddAssign or ExpressionType.SubtractAssign;

        if (!isAssign)
            Sb.Append('(');

        Visit(node.Left);
        Sb.Append(GetBinaryOperator(node.NodeType));
        Visit(node.Right);

        if (!isAssign)
            Sb.Append(')');

        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        string str = node.Value switch
        {
            char x => helper.ToValueLabel(x),
            sbyte x => helper.ToValueLabel(x),
            byte x => helper.ToValueLabel(x),
            short x => helper.ToValueLabel(x),
            ushort x => helper.ToValueLabel(x),
            int x => helper.ToValueLabel(x),
            uint x => helper.ToValueLabel(x),
            long x => helper.ToValueLabel(x),
            ulong x => helper.ToValueLabel(x),
            float x => helper.ToValueLabel(x),
            double x => helper.ToValueLabel(x),
            string x => helper.ToValueLabel(x),
            bool x => helper.ToValueLabel(x),
            _ => throw new InvalidOperationException("Unsupported type " + node.Value.GetType().Name)
        };

        Sb.Append(str);
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Sb.Append(node.Name!);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        Visit(node.Operand);
        switch (node.NodeType)
        {
            case ExpressionType.PostIncrementAssign: Sb.Append("++"); break;
            case ExpressionType.PostDecrementAssign: Sb.Append("--"); break;
            case ExpressionType.Convert: break;
            default: throw new NotSupportedException($"Unary operator {node.NodeType} is not supported.");
        }
        return node;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        Sb.Append("if (");
        Visit(node.Test);
        Sb.AppendLine(")");
        Sb.AppendLine("{");
        Sb.IncrementIndent();
        Visit(node.IfTrue);
        Sb.DecrementIndent();
        Sb.AppendLine("}");

        if (node.IfFalse != Expression.Empty())
        {
            Sb.AppendLine("else");
            Sb.AppendLine("{");
            Sb.IncrementIndent();
            Visit(node.IfFalse);
            Sb.DecrementIndent();
            Sb.AppendLine("}");
        }
        return node;
    }

    protected override Expression VisitLoop(LoopExpression node)
    {
        if (node.Body is ConditionalExpression cond &&
            cond.IfFalse is GotoExpression ge &&
            ge.Kind == GotoExpressionKind.Break)
        {
            Sb.Append("while (");
            Visit(cond.Test);
            Sb.AppendLine(")");
            Sb.AppendLine("{");
            Sb.IncrementIndent();
            Visit(cond.IfTrue);
            Sb.DecrementIndent();
            Sb.AppendLine("}");

            if (ge.Value != null)
            {
                Sb.Append("return ");
                Visit(ge.Value);
                Sb.Append(";");
            }
        }
        return node;
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        if (node.Kind == GotoExpressionKind.Break)
        {
            if (node.Value != null)
            {
                Sb.Append("return ");
                Visit(node.Value);
            }
            else Sb.Append("break");
        }
        else if (node.Kind == GotoExpressionKind.Continue)
        {
            Sb.Append("continue");
        }
        return node;
    }

    private static string GetBinaryOperator(ExpressionType type) => type switch
    {
        ExpressionType.Add => " + ",
        ExpressionType.Subtract => " - ",
        ExpressionType.Multiply => " * ",
        ExpressionType.ExclusiveOr => " ^ ",
        ExpressionType.LeftShift => " << ",
        ExpressionType.RightShift => " >> ",
        ExpressionType.Or => " | ",
        ExpressionType.GreaterThan => " > ",
        ExpressionType.GreaterThanOrEqual => " >= ",
        ExpressionType.AddAssign => " += ",
        ExpressionType.SubtractAssign => " -= ",
        ExpressionType.Assign => " = ",
        _ => throw new NotSupportedException($"Operator {type} is not supported.")
    };
}