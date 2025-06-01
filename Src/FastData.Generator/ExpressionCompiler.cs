using System.Linq.Expressions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator;

public abstract class ExpressionCompiler(TypeHelper helper) : ExpressionVisitor
{
    protected readonly FastStringBuilder Output = new FastStringBuilder();

    public string GetCode(Expression expression, int indent = 2)
    {
        Output.Clear();
        Output.Indent = indent;

        Visit(expression);
        return Output.ToString();
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
        if (node.Expression != null)
            Visit(node.Expression);
        else
            Output.Append(helper.GetTypeName(node.Member.DeclaringType!));

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

            Output.AppendLine($"{helper.GetTypeName(t)}{(v.Type.IsArray ? "[]" : "")} {v.Name};");
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
            Output.Append("(");
            Visit(node.Left);
            Output.Append(")");
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
            _ => node.Value!.ToString()
        };

        Output.Append(str);
        return node;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        string elemType = helper.GetTypeName(node.Type.GetElementType()!);
        if (node.NodeType == ExpressionType.NewArrayInit)
        {
            Output.Append("new ").Append(elemType).Append("[] {");
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                Visit(node.Expressions[i]);
                if (i < node.Expressions.Count - 1)
                    Output.Append(", ");
            }
            Output.Append("}");
        }
        else if (node.NodeType == ExpressionType.NewArrayBounds)
        {
            Output.Append("new ").Append(elemType).Append("[");
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                Visit(node.Expressions[i]);
                if (i < node.Expressions.Count - 1)
                    Output.Append(", ");
            }
            Output.Append("]");
        }
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Output.Append(node.Name!);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
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