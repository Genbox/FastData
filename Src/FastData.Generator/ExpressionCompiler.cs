using System.Linq.Expressions;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator;

public abstract class ExpressionCompiler(TypeMap map) : ExpressionVisitor
{
    protected readonly FastStringBuilder Output = new FastStringBuilder();

    public string GetCode(Expression expression, int indent = 4)
    {
        Output.Clear();
        Output.Indent = indent;

        Visit(expression);
        return Output.ToString();
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Object != null)
        {
            Visit(node.Object);
        }
        else if (node.Indexer != null)
        {
            Output.Append(map.GetTypeName(node.Indexer.DeclaringType!))
                  .Append(".");
        }

        Output.Append("[");
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Visit(node.Arguments[i]);
            if (i < node.Arguments.Count - 1)
                Output.Append(", ");
        }
        Output.Append("]");

        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            Visit(node.Object);
            Output.Append(".");
        }
        Output.Append(node.Method.Name).Append("(");
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Visit(node.Arguments[i]);
            if (i < node.Arguments.Count - 1) Output.Append(", ");
        }
        Output.Append(")");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ConstantExpression)
        {
            Output.Append(node.Member.Name);
            return node;
        }

        if (node.Expression != null)
            Visit(node.Expression);
        else
            Output.Append(map.GetTypeName(node.Member.DeclaringType!));

        Output.Append(".");
        Output.Append(node.Member.Name);
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
        {
            Type t = v.Type;

            if (v.Type.IsArray)
                t = v.Type.GetElementType()!;

            Output.AppendLine($"{map.GetTypeName(t)}{(v.Type.IsArray ? "[]" : "")} {v.Name};");
        }

        foreach (Expression expr in node.Expressions)
        {
            Visit(expr);
            if (expr is LoopExpression)
                Output.AppendLine();
            else
                Output.AppendLine(";");
        }
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            Visit(node.Left);
            Output.Append("[");
            Visit(node.Right);
            Output.Append("]");
            return node;
        }

        bool isAssign = node.NodeType is ExpressionType.Assign or ExpressionType.AddAssign or ExpressionType.SubtractAssign;

        if (!isAssign)
            Output.Append('(');

        Visit(node.Left);
        Output.Append(GetBinaryOperator(node.NodeType));
        Visit(node.Right);

        if (!isAssign)
            Output.Append(')');

        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        string str = node.Value switch
        {
            char x => map.ToValueLabel(x),
            sbyte x => map.ToValueLabel(x),
            byte x => map.ToValueLabel(x),
            short x => map.ToValueLabel(x),
            ushort x => map.ToValueLabel(x),
            int x => map.ToValueLabel(x),
            uint x => map.ToValueLabel(x),
            long x => map.ToValueLabel(x),
            ulong x => map.ToValueLabel(x),
            float x => map.ToValueLabel(x),
            double x => map.ToValueLabel(x),
            string x => map.ToValueLabel(x),
            bool x => map.ToValueLabel(x),
            _ => "unknown"
        };

        Output.Append(str);
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Output.Append(node.Name!);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert)
        {
            Output.Append("(").Append(map.GetTypeName(node.Type)).Append(")");
            Visit(node.Operand);
            return node;
        }

        Visit(node.Operand);
        switch (node.NodeType)
        {
            case ExpressionType.PostIncrementAssign: Output.Append("++"); break;
            case ExpressionType.PostDecrementAssign: Output.Append("--"); break;
            case ExpressionType.Convert: break;
            default: throw new NotSupportedException($"Unary operator {node.NodeType} is not supported.");
        }
        return node;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        Output.Append("if (");
        Visit(node.Test);
        Output.AppendLine(")");
        Output.AppendLine("{");
        Output.IncrementIndent();
        Visit(node.IfTrue);
        Output.DecrementIndent();
        Output.AppendLine("}");

        if (node.IfFalse != Expression.Empty())
        {
            Output.AppendLine("else");
            Output.AppendLine("{");
            Output.IncrementIndent();
            Visit(node.IfFalse);
            Output.DecrementIndent();
            Output.AppendLine("}");
        }
        return node;
    }

    protected override Expression VisitLoop(LoopExpression node)
    {
        if (node.Body is ConditionalExpression cond && cond.IfFalse is GotoExpression ge && ge.Kind == GotoExpressionKind.Break)
        {
            Output.Append("while (");
            Visit(cond.Test);
            Output.AppendLine(")");
            Output.AppendLine("{");
            Output.IncrementIndent();
            Visit(cond.IfTrue);
            Output.DecrementIndent();
            Output.AppendLine("}");

            if (ge.Value != null)
            {
                Output.Append("return ");
                Visit(ge.Value);
                Output.Append(";");
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
                Output.Append("return ");
                Visit(node.Value);
            }
            else Output.Append("break");
        }
        else if (node.Kind == GotoExpressionKind.Continue)
            Output.Append("continue");
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