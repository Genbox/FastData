using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Generator.CSharp;

/// <summary>Renders FastData expression trees as C# source code.</summary>
/// <param name="map">The type map used to render C# types and values.</param>
[SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException throw helper", Justification = "The netstandard2.0 target does not provide ArgumentNullException.ThrowIfNull.")]
public sealed class CSharpExpressionCompiler(TypeMap map) : ExpressionCompiler(map)
{
    private int _uncheckedContextDepth;

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (_uncheckedContextDepth == 0 && IsUncheckedBinary(node.NodeType) && IsIntegral(node.Type))
            return VisitUnchecked(node, () => base.VisitBinary(node));

        return base.VisitBinary(node);
    }

    /// <inheritdoc />
    protected override Expression VisitBlock(BlockExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (node.Expressions.Count == 0 ||
            node.Expressions[node.Expressions.Count - 1] is not ParameterExpression result ||
            !node.Variables.Any(variable => ReferenceEquals(variable, result)))
            return base.VisitBlock(node);

        // The hash template returns the block's result local explicitly; it is not a valid standalone C# statement.
        BlockExpression statements = Expression.Block(node.Variables, node.Expressions.Take(node.Expressions.Count - 1));
        base.VisitBlock(statements);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (_uncheckedContextDepth == 0 && IsUncheckedUnary(node.NodeType) && IsIntegral(node.Type))
            return VisitUnchecked(node, () => base.VisitUnary(node));

        return base.VisitUnary(node);
    }

    private Expression VisitUnchecked(Expression node, Func<Expression> visit)
    {
        Output.Append("unchecked(");
        _uncheckedContextDepth++;

        try
        {
            visit();
        }
        finally
        {
            _uncheckedContextDepth--;
            Output.Append(')');
        }

        return node;
    }

    private static bool IsIntegral(Type type) => Type.GetTypeCode(type) is TypeCode.Char or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;

    private static bool IsUncheckedBinary(ExpressionType nodeType) => nodeType is ExpressionType.Add or ExpressionType.Subtract or ExpressionType.Multiply;

    private static bool IsUncheckedUnary(ExpressionType nodeType) => nodeType == ExpressionType.Convert;
}