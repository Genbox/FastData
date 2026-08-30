using System.Linq.Expressions;

namespace Genbox.FastData.Generator.CSharp;

/// <summary>Renders FastData expression trees as C# source code.</summary>
/// <param name="map">The type map used to render C# types and values.</param>
public sealed class CSharpExpressionCompiler(TypeMap map) : ExpressionCompiler(map)
{
    private int _uncheckedContextDepth;

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        node = EnsureNotNull(node, nameof(node));

        if (_uncheckedContextDepth == 0 && IsUncheckedBinary(node.NodeType) && IsIntegral(node.Type))
            return VisitUnchecked(node, () => base.VisitBinary(node));

        return base.VisitBinary(node);
    }

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression node)
    {
        node = EnsureNotNull(node, nameof(node));

        if (_uncheckedContextDepth == 0 && IsUncheckedUnary(node.NodeType) && IsIntegral(node.Type))
            return VisitUnchecked(node, () => base.VisitUnary(node));

        return base.VisitUnary(node);
    }

    /// <inheritdoc />
    protected override Expression VisitDefault(DefaultExpression node)
    {
        Output.Append("default");
        return node;
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

    private static T EnsureNotNull<T>(T? value, string parameterName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(parameterName, "The expression cannot be null.");

        return value;
    }

    private static bool IsIntegral(Type type) => Type.GetTypeCode(type) is TypeCode.Char or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;

    private static bool IsUncheckedBinary(ExpressionType nodeType) => nodeType is ExpressionType.Add or ExpressionType.Subtract or ExpressionType.Multiply;

    private static bool IsUncheckedUnary(ExpressionType nodeType) => nodeType == ExpressionType.Convert;
}