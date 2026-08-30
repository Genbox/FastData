using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Genbox.FastData.Generator.Lowered;

internal sealed class ExpressionProgramLowerer
{
    private readonly string _compilerName;
    private readonly Type? _lambdaReturnType;
    private readonly HashSet<ParameterExpression> _activeLocals = new HashSet<ParameterExpression>();
    private readonly Stack<LoopLabels> _activeLoops = new Stack<LoopLabels>();

    private ExpressionProgramLowerer(string compilerName, Type? lambdaReturnType)
    {
        _compilerName = compilerName;
        _lambdaReturnType = lambdaReturnType;
    }

    internal static LoweredProgram LowerLambda(LambdaExpression expression, string compilerName)
    {
        ExpressionSymbolValidator.ValidateLambda(expression, compilerName);
        ExpressionProgramLowerer lowerer = new ExpressionProgramLowerer(compilerName, expression.ReturnType);
        LoweredBlock body = lowerer.LowerLambdaBody(expression.Body);
        return CreateProgram(body);
    }

    internal static LoweredProgram LowerStatements(BlockExpression expression, string compilerName)
    {
        ExpressionSymbolValidator.ValidateFragment(expression, compilerName);
        ExpressionProgramLowerer lowerer = new ExpressionProgramLowerer(compilerName, null);
        return CreateProgram(lowerer.LowerBlock(expression, BlockTailPolicy.Statements));
    }

    internal static ValidatedExpression LowerValue(Expression expression, string compilerName)
    {
        ExpressionSymbolValidator.ValidateFragment(expression, compilerName);
        ExpressionProgramLowerer lowerer = new ExpressionProgramLowerer(compilerName, null);
        return lowerer.LowerValue(expression);
    }

    private static LoweredProgram CreateProgram(LoweredBlock body)
    {
        HashSet<ParameterExpression> requiredDefaults = DefiniteAssignmentAnalyzer.Analyze(body);
        return new LoweredProgram(body, requiredDefaults);
    }

    private LoweredBlock LowerLambdaBody(Expression body)
    {
        Type returnType = _lambdaReturnType!;
        BlockExpression block = body as BlockExpression ?? Expression.Block(body);
        return LowerBlock(block, returnType == typeof(void) ? BlockTailPolicy.AppendVoidReturn : BlockTailPolicy.ReturnLastValue);
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines the active local.")]
    private LoweredBlock LowerBlock(BlockExpression block, BlockTailPolicy tailPolicy)
    {
        foreach (ParameterExpression variable in block.Variables)
            _activeLocals.Add(variable);

        try
        {
            int statementExpressionCount = tailPolicy == BlockTailPolicy.ReturnLastValue ? block.Expressions.Count - 1 : block.Expressions.Count;
            if (statementExpressionCount < 0)
                throw Unsupported(block, "A result-bearing block must contain a terminal value.");

            List<LoweredStatement> statements = new List<LoweredStatement>();
            for (int i = 0; i < statementExpressionCount; i++)
                LowerStatement(block.Expressions[i], statements);

            switch (tailPolicy)
            {
                case BlockTailPolicy.ReturnLastValue:
                    LowerReturnExpression(block.Expressions[block.Expressions.Count - 1], statements);
                    break;

                case BlockTailPolicy.AppendVoidReturn:
                    AppendVoidReturn(statements);
                    break;
            }

            return new LoweredBlock(block.Variables, statements);
        }
        finally
        {
            foreach (ParameterExpression variable in block.Variables)
                _activeLocals.Remove(variable);
        }
    }

    private void LowerReturnExpression(Expression expression, List<LoweredStatement> statements)
    {
        Type returnType = _lambdaReturnType!;
        if (expression is BlockExpression block)
        {
            LoweredBlock loweredBlock = LowerBlock(block, BlockTailPolicy.ReturnLastValue);
            AppendNestedBlock(loweredBlock, statements);
            return;
        }

        if (expression is BinaryExpression assignment && IsAssignment(assignment.NodeType))
        {
            LoweredAssignmentStatement loweredAssignment = LowerAssignment(assignment);
            statements.Add(loweredAssignment);
            ParameterExpression targetExpression = (ParameterExpression)assignment.Left;
            RequireType(targetExpression.Type, returnType, expression, "Terminal assignment target");
            statements.Add(new LoweredReturnStatement(LowerValue(targetExpression)));
            return;
        }

        if (expression is GotoExpression goTo && goTo.Kind == GotoExpressionKind.Return)
        {
            LowerGoto(goTo, statements);
            return;
        }

        if (expression is ConditionalExpression { Type: not null } conditional && conditional.Type != typeof(void))
            throw Unsupported(conditional, "Value-returning conditional expressions require value-context lowering.");

        ValidatedExpression value = LowerValue(expression);
        RequireType(value.Type, returnType, expression, "Terminal value");
        statements.Add(new LoweredReturnStatement(value));
    }

    private void LowerStatement(Expression expression, List<LoweredStatement> statements)
    {
        switch (expression)
        {
            case BlockExpression block:
                AppendNestedBlock(LowerBlock(block, BlockTailPolicy.Statements), statements);
                return;

            case BinaryExpression binary when IsAssignment(binary.NodeType):
                statements.Add(LowerAssignment(binary));
                return;

            case ConditionalExpression conditional when conditional.Type == typeof(void):
                LowerConditional(conditional, statements);
                return;

            case ConditionalExpression conditional:
                throw Unsupported(conditional, "Value-returning conditional expressions require value-context lowering.");

            case LoopExpression loop:
                LowerLoop(loop, statements);
                return;

            case GotoExpression goTo:
                LowerGoto(goTo, statements);
                return;

            case DefaultExpression { Type: var type } when type == typeof(void):
                return;

            default:
                throw Unsupported(expression, "Only assignments, control flow, and explicit returns are valid in statement context.");
        }
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines the assigned local.")]
    private LoweredAssignmentStatement LowerAssignment(BinaryExpression assignment)
    {
        ValidateBinaryOperator(assignment);

        if (assignment.Left is not ParameterExpression targetExpression)
            throw Unsupported(assignment, "Assignment targets must be variables.");

        ParameterExpression? assignedLocal = ResolveLocal(targetExpression);
        ValidatedExpression value = LowerValue(assignment.Right);
        RequireType(value.Type, targetExpression.Type, assignment, "Assignment value");

        HashSet<ParameterExpression> reads = value.LocalReads;
        if (assignment.NodeType != ExpressionType.Assign && assignedLocal != null)
            reads.Add(assignedLocal);

        ValidatedExpression operation = new ValidatedExpression(assignment, reads);
        return new LoweredAssignmentStatement(operation, assignedLocal);
    }

    private void LowerConditional(ConditionalExpression conditional, List<LoweredStatement> statements)
    {
        ValidatedExpression test = LowerValue(conditional.Test);
        RequireType(test.Type, typeof(bool), conditional.Test, "Conditional test");
        LoweredBlock ifTrue = LowerStatementAsBlock(conditional.IfTrue);
        LoweredBlock? ifFalse = conditional.IfFalse is DefaultExpression { Type: var type } && type == typeof(void)
            ? null
            : LowerStatementAsBlock(conditional.IfFalse);
        statements.Add(new LoweredIfStatement(test, ifTrue, ifFalse));
    }

    private void LowerLoop(LoopExpression loop, List<LoweredStatement> statements)
    {
        if (!TryMatchWhile(loop, out ConditionalExpression conditional, out GotoExpression breakExpression))
            throw Unsupported(loop, "Loop expression does not match the supported 'while (test) { ... } else break;' shape.");
        if (breakExpression.Value != null)
            throw Unsupported(loop, "Value-producing loops require explicit loop-result lowering.");

        ValidatedExpression test = LowerValue(conditional.Test);
        RequireType(test.Type, typeof(bool), conditional.Test, "Loop test");

        _activeLoops.Push(new LoopLabels(breakExpression.Target, loop.ContinueLabel));
        LoweredBlock body;
        try
        {
            body = LowerStatementAsBlock(conditional.IfTrue);
        }
        finally
        {
            _activeLoops.Pop();
        }

        statements.Add(new LoweredWhileStatement(test, body));
    }

    private static bool TryMatchWhile(LoopExpression loop, out ConditionalExpression conditional, out GotoExpression breakExpression)
    {
        if (loop.Body is ConditionalExpression { IfFalse: GotoExpression { Kind: GotoExpressionKind.Break } candidateBreak } candidateConditional &&
            loop.BreakLabel != null &&
            ReferenceEquals(candidateBreak.Target, loop.BreakLabel))
        {
            conditional = candidateConditional;
            breakExpression = candidateBreak;
            return true;
        }

        conditional = null!;
        breakExpression = null!;
        return false;
    }

    private void LowerGoto(GotoExpression goTo, List<LoweredStatement> statements)
    {
        switch (goTo.Kind)
        {
            case GotoExpressionKind.Return:
                statements.Add(LowerReturn(goTo.Value, goTo));
                return;

            case GotoExpressionKind.Break when TargetsCurrentBreak(goTo.Target):
                statements.Add(LoweredBreakStatement.Instance);
                return;

            case GotoExpressionKind.Continue when TargetsCurrentContinue(goTo.Target):
                statements.Add(LoweredContinueStatement.Instance);
                return;

            default:
                throw Unsupported(goTo, $"Goto kind '{goTo.Kind}' is not valid in this context.");
        }
    }

    private LoweredReturnStatement LowerReturn(Expression? expression, Expression source)
    {
        if (_lambdaReturnType == typeof(void))
        {
            if (expression != null)
                throw Unsupported(source, "A void lambda cannot return a value.");
            return new LoweredReturnStatement(null);
        }

        if (expression == null)
        {
            if (_lambdaReturnType != null)
                throw Unsupported(source, $"A lambda returning '{_lambdaReturnType}' must return a value.");
            return new LoweredReturnStatement(null);
        }

        ValidatedExpression value = LowerValue(expression);
        if (_lambdaReturnType != null)
            RequireType(value.Type, _lambdaReturnType, source, "Return value");
        return new LoweredReturnStatement(value);
    }

    private bool TargetsCurrentBreak(LabelTarget target) => _activeLoops.Count > 0 && ReferenceEquals(_activeLoops.Peek().Break, target);

    private bool TargetsCurrentContinue(LabelTarget target) => _activeLoops.Count > 0 && ReferenceEquals(_activeLoops.Peek().Continue, target);

    private LoweredBlock LowerStatementAsBlock(Expression expression)
    {
        BlockExpression block = expression as BlockExpression ?? Expression.Block(expression);
        return LowerBlock(block, BlockTailPolicy.Statements);
    }

    private ValidatedExpression LowerValue(Expression expression)
    {
        HashSet<ParameterExpression> localReads = new HashSet<ParameterExpression>();
        ValidateValueNode(expression, localReads);
        return new ValidatedExpression(expression, localReads);
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines a local read.")]
    private void ValidateValueNode(Expression expression, HashSet<ParameterExpression> localReads)
    {
        switch (expression)
        {
            case ParameterExpression parameter:
                ParameterExpression? local = ResolveLocal(parameter);
                if (local != null)
                    localReads.Add(local);
                return;

            case ConstantExpression constant:
                ValidateConstant(constant);
                return;

            case DefaultExpression defaultValue:
                ValidateDefault(defaultValue);
                return;

            case UnaryExpression unary when unary.NodeType is ExpressionType.Convert or ExpressionType.Not:
                ValidateUnaryOperator(unary);
                ValidateValueNode(unary.Operand, localReads);
                return;

            case BinaryExpression binary when binary.NodeType == ExpressionType.ArrayIndex:
                ValidateValueNode(binary.Left, localReads);
                ValidateValueNode(binary.Right, localReads);
                return;

            case BinaryExpression binary when IsValueBinary(binary.NodeType):
                ValidateBinaryOperator(binary);
                ValidateValueNode(binary.Left, localReads);
                ValidateValueNode(binary.Right, localReads);
                return;

            case IndexExpression index:
                if (index.Object != null)
                    ValidateValueNode(index.Object, localReads);
                ValidateValueNodes(index.Arguments, localReads);
                return;

            case MemberExpression { Expression: ConstantExpression }:
                return;

            case MemberExpression member:
                if (member.Expression != null)
                    ValidateValueNode(member.Expression, localReads);
                return;

            case MethodCallExpression call:
                ValidateCall(call);
                if (call.Object != null)
                    ValidateValueNode(call.Object, localReads);
                ValidateValueNodes(call.Arguments, localReads);
                return;

            case ConditionalExpression conditional when conditional.Type != typeof(void):
                throw Unsupported(conditional, "Value-returning conditional expressions require value-context lowering.");

            default:
                throw Unsupported(expression, "The expression is not part of the supported value-expression set.");
        }
    }

    private void ValidateValueNodes(IEnumerable<Expression> expressions, HashSet<ParameterExpression> localReads)
    {
        foreach (Expression expression in expressions)
            ValidateValueNode(expression, localReads);
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines the active local.")]
    private ParameterExpression? ResolveLocal(ParameterExpression expression)
        => _activeLocals.Contains(expression) ? expression : null;

    private static void AppendNestedBlock(LoweredBlock block, List<LoweredStatement> statements)
    {
        if (block.Locals.Count == 0)
        {
            foreach (LoweredStatement statement in block.Statements)
                statements.Add(statement);
        }
        else
            statements.Add(block);
    }

    private static void AppendVoidReturn(List<LoweredStatement> statements)
    {
        if (statements.Count == 0 || statements[statements.Count - 1] is not LoweredReturnStatement)
            statements.Add(new LoweredReturnStatement(null));
    }

    private void ValidateConstant(ConstantExpression constant)
    {
        if (constant.Value is Enum && constant.Type.IsEnum)
            throw Unsupported(constant, "Enum constants require target-specific lowering.");

        if (constant.Value is char or sbyte or byte or short or ushort or int or uint or long or ulong or float or double or string or bool or null)
            return;

        throw Unsupported(constant, $"Constants of type '{constant.Type}' are not supported.");
    }

    private void ValidateDefault(DefaultExpression defaultValue)
    {
        if (defaultValue.Type == typeof(object) && _lambdaReturnType == null)
            return;
        if (!defaultValue.Type.IsEnum && IsPortableDefaultType(defaultValue.Type))
            return;

        throw Unsupported(defaultValue, $"Default values of type '{defaultValue.Type}' require target-specific lowering.");
    }

    private void ValidateUnaryOperator(UnaryExpression unary)
    {
        if (unary.Method != null)
            throw Unsupported(unary, "User-defined unary operators require target-specific lowering.");
        if (unary.IsLifted || unary.IsLiftedToNull)
            throw Unsupported(unary, "Lifted unary operators require target-specific lowering.");
    }

    private void ValidateBinaryOperator(BinaryExpression binary)
    {
        if (binary.Conversion != null)
            throw Unsupported(binary, "Binary conversions require target-specific lowering.");
        if (binary.Method != null)
            throw Unsupported(binary, "User-defined binary operators require target-specific lowering.");
        if (binary.IsLifted || binary.IsLiftedToNull)
            throw Unsupported(binary, "Lifted binary operators require target-specific lowering.");
    }

    private void ValidateCall(MethodCallExpression call)
    {
        ParameterInfo[] parameters = call.Method.GetParameters();
        foreach (ParameterInfo parameter in parameters)
        {
            if (parameter.ParameterType.IsByRef)
                throw Unsupported(call, "Methods with ref or out parameters require target-specific lowering.");
        }
    }

    private void RequireType(Type actual, Type expected, Expression expression, string description)
    {
        if (actual != expected)
            throw Unsupported(expression, $"{description} has type '{actual}', but '{expected}' is required.");
    }

    private NotSupportedException Unsupported(Expression expression, string? reason = null)
    {
        string message = $"Expression node '{expression.NodeType}' ({expression.GetType().Name}) is not supported by {_compilerName}.";
        if (reason != null)
            message += " " + reason;
        return new NotSupportedException(message);
    }

    private static bool IsAssignment(ExpressionType type) => type is ExpressionType.Assign or ExpressionType.AddAssign or ExpressionType.SubtractAssign or ExpressionType.ExclusiveOrAssign;

    private static bool IsValueBinary(ExpressionType type) => type is
        ExpressionType.Add or
        ExpressionType.Subtract or
        ExpressionType.Multiply or
        ExpressionType.ExclusiveOr or
        ExpressionType.LeftShift or
        ExpressionType.RightShift or
        ExpressionType.Or or
        ExpressionType.And or
        ExpressionType.AndAlso or
        ExpressionType.OrElse or
        ExpressionType.Modulo or
        ExpressionType.Equal or
        ExpressionType.NotEqual or
        ExpressionType.LessThan or
        ExpressionType.LessThanOrEqual or
        ExpressionType.GreaterThan or
        ExpressionType.GreaterThanOrEqual;

    private static bool IsPortableDefaultType(Type type) => Type.GetTypeCode(type) is
        TypeCode.Boolean or
        TypeCode.Char or
        TypeCode.SByte or
        TypeCode.Byte or
        TypeCode.Int16 or
        TypeCode.UInt16 or
        TypeCode.Int32 or
        TypeCode.UInt32 or
        TypeCode.Int64 or
        TypeCode.UInt64 or
        TypeCode.Single or
        TypeCode.Double;

    private enum BlockTailPolicy : byte
    {
        Statements,
        ReturnLastValue,
        AppendVoidReturn
    }

    private readonly record struct LoopLabels(LabelTarget Break, LabelTarget? Continue);
}