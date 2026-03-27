using System.Linq.Expressions;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal;

public sealed class RustExpressionCompiler(TypeMap map) : ExpressionCompiler(map)
{
    protected override Expression VisitBlock(BlockExpression node)
    {
        foreach (ParameterExpression v in node.Variables)
        {
            Type t = v.Type;

            if (v.Type.IsArray)
                t = v.Type.GetElementType()!;

            Output.AppendLine($"let mut {v.Name}: {map.GetTypeName(t)}{(v.Type.IsArray ? "[]" : string.Empty)};");
        }

        foreach (Expression expr in node.Expressions)
        {
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
            Output.Append("Self::").Append(node.Member.Name);
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
            Output.Append("]");
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
            if (ch <= byte.MaxValue)
            {
                Output.Append(((byte)ch).ToString(CultureInfo.InvariantCulture)).Append("u8");
                return node;
            }
        }

        return base.VisitConstant(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
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

        if (node.NodeType == ExpressionType.Subtract && IsUnsigned(node.Type))
        {
            Output.Append('(');
            Visit(node.Left);
            Output.Append(".wrapping_sub(");
            Visit(node.Right);
            Output.Append("))");
            return node;
        }

        return base.VisitBinary(node);
    }

    private static bool IsUnsigned(Type type) => type == typeof(byte)
                                                 || type == typeof(ushort)
                                                 || type == typeof(uint)
                                                 || type == typeof(ulong);

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