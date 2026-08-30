using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Genbox.FastData.Generator;

/// <summary>Converts expression trees used by FastData into target-language source fragments.</summary>
[SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.")]
[SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException throw helper", Justification = "The netstandard2.0 target does not provide ArgumentNullException.ThrowIfNull.")]
public abstract class ExpressionCompiler(TypeMap map) : ExpressionVisitor
{
    /// <summary>Gets the type map used to render target-language types and values.</summary>
    protected TypeMap Map { get; } = map ?? throw new ArgumentNullException(nameof(map));

    /// <summary>Gets the builder that receives generated source code.</summary>
    protected IndentedStringBuilder Output { get; } = new IndentedStringBuilder();

    /// <summary>Renders an expression tree to source code.</summary>
    /// <param name="expression">The expression tree to render.</param>
    /// <param name="indent">The starting indentation level.</param>
    /// <returns>The rendered source fragment.</returns>
    public string GetCode(Expression expression, int indent = 0)
    {
        Output.Clear();
        Output.Indent = indent;

        Visit(expression);
        return Output.ToString();
    }

    /// <inheritdoc />
    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (node.Object != null)
            Visit(node.Object);
        else if (node.Indexer != null)
        {
            Output.Append(Map.GetTypeName(node.Indexer.DeclaringType!))
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

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

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

    /// <inheritdoc />
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (node.Expression is ConstantExpression)
        {
            Output.Append(node.Member.Name);
            return node;
        }

        if (node.Expression != null)
            Visit(node.Expression);
        else
            Output.Append(Map.GetTypeName(node.Member.DeclaringType!));

        Output.Append(".");
        Output.Append(node.Member.Name);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        Visit(node.Body);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitBlock(BlockExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        // Build a map from variable to its initializer expression, so we can emit combined declaration-with-initializer statements (e.g. "int length = Length(key);")
        // instead of separate declaration and assignment lines.
        Dictionary<ParameterExpression, Expression> initializers = new Dictionary<ParameterExpression, Expression>();
        HashSet<Expression> inlinedExprs = new HashSet<Expression>();

        foreach (Expression expr in node.Expressions)
        {
            if (expr is BinaryExpression { NodeType: ExpressionType.Assign, Left: ParameterExpression left } assign
                && node.Variables.Contains(left)
                && !initializers.ContainsKey(left))
            {
                initializers[left] = assign.Right;
                inlinedExprs.Add(expr);
                continue;
            }

            break;
        }

        foreach (ParameterExpression v in node.Variables)
        {
            Type t = v.Type;

            if (v.Type.IsArray)
                t = v.Type.GetElementType()!;

            string typeName = $"{Map.GetTypeName(t)}{(v.Type.IsArray ? "[]" : "")}";

            initializers.TryGetValue(v, out Expression? init);
            WriteVariableDeclaration(v, typeName, init);
        }

        foreach (Expression expr in node.Expressions)
        {
            if (inlinedExprs.Contains(expr))
                continue;

            Visit(expr);
            if (expr is LoopExpression or ConditionalExpression)
                Output.AppendLine();
            else
                Output.AppendLine(";");
        }
        return node;
    }

    /// <summary>Renders a single block-scoped variable declaration, with its initializer if one was inlined.</summary>
    protected virtual void WriteVariableDeclaration(ParameterExpression v, string typeName, Expression? init)
    {
        if (v == null)
            throw new ArgumentNullException(nameof(v));

        if (init != null)
        {
            Output.Append($"{typeName} {v.Name} = ");
            Visit(init);
            Output.AppendLine(";");
        }
        else
            Output.AppendLine($"{typeName} {v.Name};");
    }

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            Visit(node.Left);
            Output.Append("[");
            Visit(node.Right);
            Output.Append("]");
            return node;
        }

        bool isAssign = node.NodeType is ExpressionType.Assign or ExpressionType.AddAssign or ExpressionType.SubtractAssign or ExpressionType.ExclusiveOrAssign;

        if (!isAssign)
            Output.Append('(');

        Visit(node.Left);
        Output.Append(GetBinaryOperator(node.NodeType));
        Visit(node.Right);

        if (!isAssign)
            Output.Append(')');

        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (node.Value is Enum && node.Type.IsEnum)
        {
            Output.Append(node.Type.Name).Append(".").Append(node.Value.ToString()!);
            return node;
        }

        string str = node.Value switch
        {
            char x => Map.GetValueLiteral(x),
            sbyte x => Map.GetValueLiteral(x),
            byte x => Map.GetValueLiteral(x),
            short x => Map.GetValueLiteral(x),
            ushort x => Map.GetValueLiteral(x),
            int x => Map.GetValueLiteral(x),
            uint x => Map.GetValueLiteral(x),
            long x => Map.GetValueLiteral(x),
            ulong x => Map.GetValueLiteral(x),
            float x => Map.GetValueLiteral(x),
            double x => Map.GetValueLiteral(x),
            string x => Map.GetValueLiteral(x),
            bool x => Map.GetValueLiteral(x),
            null => Map.GetValueLiteral(null),
            _ => throw new NotSupportedException($"Constants of type '{node.Type}' are not supported.")
        };

        Output.Append(str);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        Output.Append(node.Name!);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (node.NodeType == ExpressionType.Convert)
        {
            Output.Append("(").Append(Map.GetTypeName(node.Type)).Append(")");
            Visit(node.Operand);
            return node;
        }

        if (node.NodeType == ExpressionType.Not)
        {
            Output.Append(node.Type == typeof(bool) ? "!" : "~");
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

    /// <inheritdoc />
    protected override Expression VisitConditional(ConditionalExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        Output.Append("if (");
        Visit(node.Test);
        Output.AppendLine(")");
        Output.AppendLine("{");
        Output.IncrementIndent();
        VisitStatement(node.IfTrue);
        Output.DecrementIndent();
        Output.AppendLine("}");

        if (node.IfFalse is not DefaultExpression || node.IfFalse.Type != typeof(void))
        {
            Output.AppendLine("else");
            Output.AppendLine("{");
            Output.IncrementIndent();
            VisitStatement(node.IfFalse);
            Output.DecrementIndent();
            Output.AppendLine("}");
        }
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitLoop(LoopExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (node.Body is not ConditionalExpression { IfFalse: GotoExpression { Kind: GotoExpressionKind.Break } ge } cond)
            throw new NotSupportedException($"Loop expression does not match the supported 'while (test) {{ ... }} break;' shape: {node}");

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
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitGoto(GotoExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        switch (node.Kind)
        {
            case GotoExpressionKind.Break:
                if (node.Value != null)
                {
                    Output.Append("return ");
                    Visit(node.Value);
                }
                else
                    Output.Append("break");
                break;

            case GotoExpressionKind.Continue:
                Output.Append("continue");
                break;

            default:
                throw new NotSupportedException($"Goto kind {node.Kind} is not supported.");
        }
        return node;
    }

    private void VisitStatement(Expression node)
    {
        Visit(node);
        if (node is not BlockExpression and not LoopExpression and not ConditionalExpression)
            Output.AppendLine(";");
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
        ExpressionType.And => " & ",
        ExpressionType.AndAlso => " && ",
        ExpressionType.OrElse => " || ",
        ExpressionType.Modulo => " % ",
        ExpressionType.Equal => " == ",
        ExpressionType.NotEqual => " != ",
        ExpressionType.LessThan => " < ",
        ExpressionType.LessThanOrEqual => " <= ",
        ExpressionType.GreaterThan => " > ",
        ExpressionType.GreaterThanOrEqual => " >= ",
        ExpressionType.AddAssign => " += ",
        ExpressionType.SubtractAssign => " -= ",
        ExpressionType.ExclusiveOrAssign => " ^= ",
        ExpressionType.Assign => " = ",
        _ => throw new NotSupportedException($"Operator {type} is not supported.")
    };
}