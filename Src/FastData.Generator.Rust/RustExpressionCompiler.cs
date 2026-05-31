using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.Rust;

[SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.")]
public sealed class RustExpressionCompiler(TypeMap map) : ExpressionCompiler(map)
{
    protected override Expression VisitBlock(BlockExpression node)
    {
        // Build a map from variable to its initializer expression, so we can emit
        // combined declaration-with-initializer statements (e.g. "let mut length: i32 = Length(key);")
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
            }
        }

        foreach (ParameterExpression v in node.Variables)
        {
            Type t = v.Type;

            if (v.Type.IsArray)
                t = v.Type.GetElementType()!;

            string typeName = $"{map.GetTypeName(t)}{(v.Type.IsArray ? "[]" : string.Empty)}";

            if (initializers.TryGetValue(v, out Expression? init))
            {
                Output.Append($"let mut {v.Name}: {typeName} = ");
                Visit(init);
                Output.AppendLine(";");
            }
            else
            {
                Output.AppendLine($"let mut {v.Name}: {typeName};");
            }
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

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ConstantExpression)
        {
            // Captured constant members (e.g. GPerf association values) are emitted at
            // module scope in the Rust template, not inside impl, so no Self:: prefix.
            Output.Append(node.Member.Name);
            return node;
        }

        if (node.Expression != null && node.Member.Name == nameof(string.Length) && node.Expression.Type == typeof(string))
        {
            Output.Append("(");
            Visit(node.Expression);
            Output.Append(".len() as i32)");
            return node;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Object != null && node.Object.Type == typeof(string) && node.Indexer?.Name == "Chars")
        {
            Visit(node.Object);
            Output.Append(".as_bytes()[");
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Output.Append("(");
                Visit(node.Arguments[i]);
                Output.Append(" as usize)");
                if (i < node.Arguments.Count - 1)
                    Output.Append(", ");
            }
            Output.Append("] as char");
            return node;
        }

        return base.VisitIndex(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(string))
        {
            if (node.Method.Name == nameof(string.StartsWith))
                return RenderStringCall(node, "starts_with");
            if (node.Method.Name == nameof(string.EndsWith))
                return RenderStringCall(node, "ends_with");
        }

        // ReadU8/U16/U32/U64: the offset argument is int in the expression tree but
        // Rust helpers require usize, so cast it.
        if (node.Method.DeclaringType == typeof(GeneratorFunctions) && node.Method.Name.StartsWith("Read", StringComparison.Ordinal))
        {
            Output.Append(node.Method.Name).Append("(");
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0)
                    Output.Append(", ");

                Visit(node.Arguments[i]);

                // The second argument (offset) needs a usize cast
                if (i == 1)
                    Output.Append(" as usize");
            }
            Output.Append(")");
            return node;
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not)
        {
            Output.Append("!");
            Visit(node.Operand);
            return node;
        }

        if (node.NodeType == ExpressionType.Convert)
        {
            Output.Append("(");
            Visit(node.Operand);
            Output.Append(" as ").Append(map.GetTypeName(node.Type)).Append(")");
            return node;
        }

        return base.VisitUnary(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is char ch)
        {
            Output.Append(map.ToValueLabel(ch));
            return node;
        }

        return base.VisitConstant(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            Visit(node.Left);
            Output.Append("[(");
            Visit(node.Right);
            Output.Append(") as usize]");
            return node;
        }

        if (node.NodeType is ExpressionType.LeftShift or ExpressionType.RightShift)
        {
            Output.Append('(');
            Visit(node.Left);
            Output.Append(node.NodeType == ExpressionType.LeftShift ? " << " : " >> ");
            Output.Append('(');
            Visit(node.Right);
            Output.Append(" as u32))");
            return node;
        }

        // Unsigned arithmetic must use wrapping methods in Rust to avoid panic on overflow.
        if (IsUnsigned(node.Type))
        {
            string? wrappingMethod = node.NodeType switch
            {
                ExpressionType.Add => "wrapping_add",
                ExpressionType.Subtract => "wrapping_sub",
                ExpressionType.Multiply => "wrapping_mul",
                _ => null
            };

            if (wrappingMethod != null)
            {
                Output.Append('(');
                VisitWrappingReceiver(node.Left, node.Type);
                Output.Append('.').Append(wrappingMethod).Append('(');
                Visit(node.Right);
                Output.Append("))");
                return node;
            }

            // Compound assignments: lhs = lhs.wrapping_op(rhs)
            string? wrappingAssignMethod = node.NodeType switch
            {
                ExpressionType.AddAssign => "wrapping_add",
                ExpressionType.SubtractAssign => "wrapping_sub",
                _ => null
            };

            if (wrappingAssignMethod != null)
            {
                Visit(node.Left);
                Output.Append(" = ");
                Visit(node.Left);
                Output.Append('.').Append(wrappingAssignMethod).Append('(');
                Visit(node.Right);
                Output.Append(")");
                return node;
            }
        }

        return base.VisitBinary(node);
    }

    private static bool IsUnsigned(Type type) => type == typeof(byte)
                                                 || type == typeof(ushort)
                                                 || type == typeof(uint)
                                                 || type == typeof(ulong);

    private void VisitWrappingReceiver(Expression expression, Type type)
    {
        if (expression is ConstantExpression)
        {
            Output.Append('(');
            Visit(expression);
            Output.Append(" as ").Append(map.GetTypeName(type)).Append(')');
            return;
        }

        Visit(expression);
    }

    private Expression RenderStringCall(MethodCallExpression node, string methodName)
    {
        if (node.Object != null && node.Arguments.Count > 0)
        {
            Visit(node.Object);
            Output.Append(".").Append(methodName).Append("(");

            if (node.Arguments[0] is ConstantExpression constExpr && constExpr.Value is string literal)
                Output.Append(map.ToValueLabel(literal));
            else
                Visit(node.Arguments[0]);

            Output.Append(")");
            return node;
        }

        return base.VisitMethodCall(node);
    }
}