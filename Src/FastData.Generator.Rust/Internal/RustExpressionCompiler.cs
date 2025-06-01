using System.Linq.Expressions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.Rust.Internal;

internal sealed class RustExpressionCompiler(TypeHelper helper) : ExpressionCompiler(helper)
{
    private readonly TypeHelper _helper = helper;

    protected override Expression VisitBlock(BlockExpression node)
    {
        foreach (ParameterExpression v in node.Variables)
            Output.AppendLine($"let mut {_helper.GetTypeName(v.Type)} {v.Name};");

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

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null)
        {
            Visit(node.Expression);
            Output.Append(".");
        }

        string name = node.Member.Name;

        if (name == "Length")
            name = "len()";

        Output.Append(name);
        return node;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        // Rust if-expression
        Output.Append("if ");
        Visit(node.Test);
        Output.Append(" { ");
        Visit(node.IfTrue);
        Output.Append(" } else { ");
        Visit(node.IfFalse);
        Output.Append(" }");
        return node;
    }
}