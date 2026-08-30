using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Generator.Lowered;

namespace Genbox.FastData.Generator;

/// <summary>Converts expression trees used by FastData into target-language source fragments.</summary>
public abstract class ExpressionCompiler(TypeMap map) : ExpressionVisitor
{
    /// <summary>The output buffer used by derived expression compilers.</summary>
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This protected field is part of the established derived-compiler API.")]
    protected readonly IndentedStringBuilder Output = new IndentedStringBuilder();

    /// <summary>The target-language type map.</summary>
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This protected field is part of the established derived-compiler API.")]
    protected readonly TypeMap Map = map;

    /// <summary>Renders a lambda as a complete returning method body.</summary>
    public string GetLambdaBody(LambdaExpression expression, int indent = 0)
    {
        expression = EnsureNotNull(expression, nameof(expression));

        string code = RenderProgram(ExpressionProgramLowerer.LowerLambda(expression, GetType().Name), indent);
        return code.TrimEnd('\r', '\n');
    }

    /// <summary>Renders a block expression as a statement fragment.</summary>
    public string GetStatements(BlockExpression expression, int indent = 0) => RenderProgram(ExpressionProgramLowerer.LowerStatements(expression, GetType().Name), indent);

    /// <summary>Renders one validated value expression.</summary>
    public string GetValue(Expression expression)
    {
        ValidatedExpression value = ExpressionProgramLowerer.LowerValue(expression, GetType().Name);
        Output.Clear();
        Output.Indent = 0;
        RenderValue(value);
        return Output.ToString();
    }

    private static bool IsAssignment(ExpressionType type) => type is ExpressionType.Assign or ExpressionType.AddAssign or ExpressionType.SubtractAssign or ExpressionType.ExclusiveOrAssign;

    private string RenderProgram(LoweredProgram program, int indent)
    {
        Output.Clear();
        Output.Indent = indent;
        RenderBlock(program, program.Body);
        return Output.ToString();
    }

    private void RenderBlock(LoweredProgram program, LoweredBlock block)
    {
        foreach (ParameterExpression local in block.Locals)
        {
            Type type = local.Type;
            Type elementType = type.IsArray ? type.GetElementType()! : type;
            string typeName = $"{Map.GetTypeName(elementType)}{(type.IsArray ? "[]" : "")}";
            Expression? initializer = program.RequiresDefaultInitialization(local) ? CreateDefaultValue(type) : null;
            WriteVariableDeclaration(local, typeName, initializer);
        }

        foreach (LoweredStatement statement in block.Statements)
        {
            RenderStatement(program, statement);
            if (statement is LoweredIfStatement or LoweredWhileStatement)
                Output.AppendLine();
        }
    }

    private void RenderStatement(LoweredProgram program, LoweredStatement statement)
    {
        switch (statement)
        {
            case LoweredAssignmentStatement assignment:
                RenderValue(assignment.Operation);
                Output.AppendLine(";");
                return;

            case LoweredIfStatement conditional:
                Output.Append("if (");
                RenderValue(conditional.Test);
                Output.AppendLine(")");
                Output.AppendLine("{");
                Output.IncrementIndent();
                RenderBlock(program, conditional.IfTrue);
                Output.DecrementIndent();
                Output.AppendLine("}");

                if (conditional.IfFalse != null)
                {
                    Output.AppendLine("else");
                    Output.AppendLine("{");
                    Output.IncrementIndent();
                    RenderBlock(program, conditional.IfFalse);
                    Output.DecrementIndent();
                    Output.AppendLine("}");
                }
                return;

            case LoweredWhileStatement loop:
                Output.Append("while (");
                RenderValue(loop.Test);
                Output.AppendLine(")");
                Output.AppendLine("{");
                Output.IncrementIndent();
                RenderBlock(program, loop.Body);
                Output.DecrementIndent();
                Output.AppendLine("}");
                return;

            case LoweredBlock block:
                Output.AppendLine("{");
                Output.IncrementIndent();
                RenderBlock(program, block);
                Output.DecrementIndent();
                Output.AppendLine("}");
                return;

            case LoweredReturnStatement returnStatement:
                Output.Append("return");
                if (returnStatement.Value != null)
                {
                    Output.Append(" ");
                    RenderValue(returnStatement.Value);
                }
                Output.AppendLine(";");
                return;

            case LoweredBreakStatement:
                Output.AppendLine("break;");
                return;

            case LoweredContinueStatement:
                Output.AppendLine("continue;");
                return;

            default:
                throw new InvalidOperationException($"Unknown lowered statement '{statement.GetType().Name}'.");
        }
    }

    private void RenderValue(ValidatedExpression value) => Visit(value.Source);

    private ConstantExpression CreateDefaultValue(Type type)
    {
        if (!type.IsValueType || type.IsEnum)
            throw new NotSupportedException($"Default initialization of local type '{type}' requires target-specific lowering in {GetType().Name}.");

        object value = Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => false,
            TypeCode.Char => '\0',
            TypeCode.SByte => (sbyte)0,
            TypeCode.Byte => (byte)0,
            TypeCode.Int16 => (short)0,
            TypeCode.UInt16 => (ushort)0,
            TypeCode.Int32 => 0,
            TypeCode.UInt32 => 0U,
            TypeCode.Int64 => 0L,
            TypeCode.UInt64 => 0UL,
            TypeCode.Single => 0F,
            TypeCode.Double => 0D,
            _ => throw new NotSupportedException($"Default initialization of local type '{type}' is not supported by {GetType().Name}.")
        };
        return Expression.Constant(value, type);
    }

    /// <inheritdoc />
    protected override Expression VisitIndex(IndexExpression node)
    {
        node = EnsureNotNull(node, nameof(node));

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
        node = EnsureNotNull(node, nameof(node));

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
        node = EnsureNotNull(node, nameof(node));

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
        => UnsupportedNode(node, "Lambda expressions must be lowered before rendering.");

    /// <inheritdoc />
    protected override Expression VisitBlock(BlockExpression node) => UnsupportedNode(node, "Block expressions must be lowered before rendering.");

    /// <summary>Renders a single block-scoped variable declaration, with a required default initializer when needed.</summary>
    protected virtual void WriteVariableDeclaration(ParameterExpression v, string typeName, Expression? init)
    {
        v = EnsureNotNull(v, nameof(v));

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
        node = EnsureNotNull(node, nameof(node));

        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            Visit(node.Left);
            Output.Append("[");
            Visit(node.Right);
            Output.Append("]");
            return node;
        }

        bool isAssign = IsAssignment(node.NodeType);

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
        node = EnsureNotNull(node, nameof(node));

        if (node.Value is Enum && node.Type.IsEnum)
            return UnsupportedNode(node, "Enum constants require target-specific lowering.");

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
        node = EnsureNotNull(node, nameof(node));
        Output.Append(node.Name!);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression node)
    {
        node = EnsureNotNull(node, nameof(node));

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

        return UnsupportedNode(node, $"Unary operator {node.NodeType} is not supported.");
    }

    /// <inheritdoc />
    protected override Expression VisitConditional(ConditionalExpression node) => UnsupportedNode(node, "Conditional expressions must be lowered before rendering.");

    /// <inheritdoc />
    protected override Expression VisitDebugInfo(DebugInfoExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitDefault(DefaultExpression node)
    {
        node = EnsureNotNull(node, nameof(node));

        if (node.Type == typeof(object))
            Output.Append(Map.GetValueLiteral(null));
        else
            Visit(CreateDefaultValue(node.Type));
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitDynamic(DynamicExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitInvocation(InvocationExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitLabel(LabelExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitListInit(ListInitExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitMemberInit(MemberInitExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitNew(NewExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitNewArray(NewArrayExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitSwitch(SwitchExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitTry(TryExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitTypeBinary(TypeBinaryExpression node) => UnsupportedNode(node);

    /// <inheritdoc />
    protected override Expression VisitLoop(LoopExpression node) => UnsupportedNode(node, "Loop expressions must be lowered before rendering.");

    /// <inheritdoc />
    protected override Expression VisitGoto(GotoExpression node) => UnsupportedNode(node, "Goto expressions must be lowered before rendering.");

    private Expression UnsupportedNode(Expression node, string? reason = null)
    {
        node = EnsureNotNull(node, nameof(node));
        string message = $"Expression node '{node.NodeType}' ({node.GetType().Name}) is not supported by {GetType().Name}.";
        if (reason != null)
            message += " " + reason;
        throw new NotSupportedException(message);
    }

    private static T EnsureNotNull<T>(T? value, string parameterName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(parameterName, "The expression cannot be null.");

        return value;
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