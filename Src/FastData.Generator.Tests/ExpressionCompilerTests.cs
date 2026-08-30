using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;
using Genbox.FastData.Generator.Lowered;

namespace Genbox.FastData.Generator.Tests;

public class ExpressionCompilerTests
{
    [Fact]
    public void BinaryExpressionRendersOperators()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression parameter = Expression.Parameter(typeof(int), "input");
        Expression expression = Expression.Equal(parameter, Expression.Constant(3));

        string output = compiler.GetValue(expression);

        Assert.Equal("(input == 3)", output);
    }

    [Fact]
    public void NonVariableAssignmentTargetIsRejected()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression values = Expression.Parameter(typeof(int[]), "values");
        BinaryExpression elementAssignment = Expression.Assign(Expression.ArrayAccess(values, Expression.Constant(0)), Expression.Constant(1));
        BlockExpression statements = Expression.Block(elementAssignment);

        NotSupportedException targetException = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(statements));

        Assert.Contains("Assignment targets must be variables.", targetException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ArrayIndexRendersBrackets()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression parameter = Expression.Parameter(typeof(int[]), "numbers");
        Expression expression = Expression.ArrayIndex(parameter, Expression.Constant(1));

        string output = compiler.GetValue(expression);

        Assert.Equal("numbers[1]", output);
    }

    [Fact]
    public void MethodCallRendersMemberInvocation()
    {
        TestCompiler compiler = CreateCompiler();
        MethodInfo method = typeof(string).GetMethod(
            nameof(string.StartsWith),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            null,
            [typeof(string)],
            null)!;
        Expression expression = Expression.Call(Expression.Constant("hello"), method, Expression.Constant("he"));

        string output = compiler.GetValue(expression);

        Assert.Equal("\"hello\".StartsWith(\"he\")", output);
    }

    [Fact]
    public void RefOrOutMethodCallsRequireTargetSpecificLowering()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        MethodInfo method = typeof(ExpressionCompilerTests).GetMethod(
            nameof(TryAssign),
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly,
            null,
            [typeof(int).MakeByRefType()],
            null)!;
        LambdaExpression expression = Expression.Lambda<Func<bool>>(Expression.Block([local], Expression.Call(method, local)));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(expression));

        Assert.Contains("Methods with ref or out parameters require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssignmentsRenderAfterDeclarationsInOriginalOrder()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression first = Expression.Variable(typeof(int), "first");
        ParameterExpression second = Expression.Variable(typeof(int), "second");
        BlockExpression expression = Expression.Block(
            [first, second],
            Expression.Assign(first, Expression.Constant(1)),
            Expression.Assign(second, first));

        string output = compiler.GetStatements(expression);

        Assert.Equal(
            ["int first;", "int second;", "first = 1;", "second = first;"],
            output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void ForwardLocalReferenceRemainsOrderedAndGetsRequiredDefault()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression first = Expression.Variable(typeof(int), "first");
        ParameterExpression second = Expression.Variable(typeof(int), "second");
        BlockExpression expression = Expression.Block(
            [first, second],
            Expression.Assign(first, second),
            Expression.Assign(second, Expression.Constant(2)));

        string output = compiler.GetStatements(expression);

        Assert.Equal(
            ["int first;", "int second = 0;", "first = second;", "second = 2;"],
            output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void LocalReadBeforeAssignmentGetsTargetDefault()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression first = Expression.Variable(typeof(int), "first");
        ParameterExpression second = Expression.Variable(typeof(int), "second");
        BlockExpression expression = Expression.Block(
            [first, second],
            Expression.Assign(second, first),
            Expression.Assign(first, Expression.Constant(1)));

        string output = compiler.GetStatements(expression);

        Assert.Equal(
            ["int first = 0;", "int second;", "second = first;", "first = 1;"],
            output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void NonPortableLocalDefaultIsRejectedClearly()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression local = Expression.Variable(typeof(object), "local");
        LambdaExpression expression = Expression.Lambda<Func<object>>(Expression.Block([local], local));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(expression));

        Assert.Contains("Default initialization of local type 'System.Object' requires target-specific lowering", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BothConditionalBranchesAssignLocalWithoutDefault()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression condition = Expression.Parameter(typeof(bool), "condition");
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        ConditionalExpression conditional = Expression.IfThenElse(
            condition,
            CreateVoidAssignment(local, 1),
            CreateVoidAssignment(local, 2));
        LambdaExpression expression = Expression.Lambda<Func<bool, int>>(Expression.Block([local], conditional, local), condition);

        string output = compiler.GetLambdaBody(expression);

        Assert.Contains("int local;", output, StringComparison.Ordinal);
        Assert.DoesNotContain("int local = 0;", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ConditionalMissingAssignmentGetsDefault()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression condition = Expression.Parameter(typeof(bool), "condition");
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        ConditionalExpression conditional = Expression.IfThen(condition, CreateVoidAssignment(local, 1));
        LambdaExpression expression = Expression.Lambda<Func<bool, int>>(Expression.Block([local], conditional, local), condition);

        string output = compiler.GetLambdaBody(expression);

        Assert.Contains("int local = 0;", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ReturningConditionalArmDoesNotRequireDefaultFromThatPath()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression condition = Expression.Parameter(typeof(bool), "condition");
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        LabelTarget returnTarget = Expression.Label(typeof(int));
        ConditionalExpression conditional = Expression.IfThenElse(
            condition,
            Expression.Return(returnTarget, Expression.Constant(0)),
            CreateVoidAssignment(local, 1));
        LambdaExpression expression = Expression.Lambda<Func<bool, int>>(Expression.Block([local], conditional, local), condition);

        string output = compiler.GetLambdaBody(expression);

        Assert.Contains("int local;", output, StringComparison.Ordinal);
        Assert.DoesNotContain("int local = 0;", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ZeroIterationLoopAssignmentDoesNotEstablishDefiniteAssignment()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression condition = Expression.Parameter(typeof(bool), "condition");
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        LabelTarget breakTarget = Expression.Label();
        ConditionalExpression loopBody = Expression.IfThenElse(condition, CreateVoidAssignment(local, 1), Expression.Break(breakTarget));
        LoopExpression loop = Expression.Loop(loopBody, breakTarget);
        LambdaExpression expression = Expression.Lambda<Func<bool, int>>(Expression.Block([local], loop, local), condition);

        string output = compiler.GetLambdaBody(expression);

        Assert.Contains("int local = 0;", output, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedScopeAssignmentEstablishesOuterDefiniteAssignment()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression outer = Expression.Variable(typeof(int), "outer");
        ParameterExpression inner = Expression.Variable(typeof(int), "inner");
        BlockExpression scope = Expression.Block(
            [inner],
            Expression.Assign(inner, Expression.Constant(1)),
            Expression.Assign(outer, inner));
        LambdaExpression expression = Expression.Lambda<Func<int>>(Expression.Block([outer], scope, outer));

        string output = compiler.GetLambdaBody(expression);

        Assert.Contains("int outer;", output, StringComparison.Ordinal);
        Assert.DoesNotContain("int outer = 0;", output, StringComparison.Ordinal);
    }

    [Fact]
    public void CompoundAssignmentReadsAndDefaultsItsTarget()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression input = Expression.Parameter(typeof(int), "input");
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        LambdaExpression expression = Expression.Lambda<Func<int, int>>(
            Expression.Block([local], Expression.AddAssign(local, input), local),
            input);

        string output = compiler.GetLambdaBody(expression);

        Assert.Contains("int local = 0;", output, StringComparison.Ordinal);
        Assert.Contains("local += input;", output, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidLambdaEmitsOneExplicitReturn()
    {
        TestCompiler compiler = CreateCompiler();
        LambdaExpression expression = Expression.Lambda<Action>(Expression.Empty());

        string output = compiler.GetLambdaBody(expression);

        Assert.Equal("return;", output);
    }

    [Fact]
    public void DuplicateSymbolNamesThrowClearException()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression first = Expression.Variable(typeof(int), "value");
        ParameterExpression second = Expression.Variable(typeof(int), "value");
        BlockExpression expression = Expression.Block([first, second], Expression.Assign(first, second));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(expression));

        Assert.Contains("Expression symbol name 'value' refers to multiple symbol identities", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SymbolNameCannotShadowCapturedExternalData()
    {
        TestCompiler compiler = CreateCompiler();
        CapturedData source = new CapturedData(1);
        ParameterExpression input = Expression.Parameter(typeof(int), nameof(CapturedData.Value));
        MemberExpression captured = Expression.Property(Expression.Constant(source), nameof(CapturedData.Value));
        LambdaExpression expression = Expression.Lambda<Func<int, int>>(Expression.Add(input, captured), input);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(expression));

        Assert.Contains("collides with captured external data", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsupportedNodeThrowsClearException()
    {
        TestCompiler compiler = CreateCompiler();
        NewExpression expression = Expression.New(typeof(object));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetValue(expression));

        Assert.Contains("Expression node 'New' (NewExpression) is not supported by TestCompiler.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValueConditionalThrowsClearException()
    {
        TestCompiler compiler = CreateCompiler();
        ConditionalExpression expression = Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant(2));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetValue(expression));

        Assert.Contains("Value-returning conditional expressions require value-context lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LambdaLoweringMakesTerminalAssignmentAndReturnExplicit()
    {
        ParameterExpression input = Expression.Parameter(typeof(int), "input");
        ParameterExpression result = Expression.Variable(typeof(int), "result");
        BinaryExpression assignment = Expression.Assign(result, input);
        LambdaExpression expression = Expression.Lambda<Func<int, int>>(
            Expression.Block([result], assignment),
            input);

        LoweredProgram program = ExpressionProgramLowerer.LowerLambda(expression, nameof(TestCompiler));

        Assert.Same(result, Assert.Single(program.Body.Locals));
        Assert.Collection(
            program.Body.Statements,
            statement =>
            {
                LoweredAssignmentStatement loweredAssignment = Assert.IsType<LoweredAssignmentStatement>(statement);
                Assert.Same(assignment, loweredAssignment.Operation.Source);
                Assert.Same(result, loweredAssignment.AssignedLocal);
                Assert.Empty(loweredAssignment.Operation.LocalReads);
            },
            statement =>
            {
                LoweredReturnStatement returnStatement = Assert.IsType<LoweredReturnStatement>(statement);
                ValidatedExpression value = Assert.IsType<ValidatedExpression>(returnStatement.Value);
                Assert.Same(result, value.Source);
                Assert.Same(result, Assert.Single(value.LocalReads));
            });
    }

    [Fact]
    public void ValidatedExpressionContainsEachLocalReadOnce()
    {
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        BinaryExpression sum = Expression.Add(local, local);
        LambdaExpression expression = Expression.Lambda<Func<int>>(
            Expression.Block([local], Expression.Assign(local, Expression.Constant(1)), sum));

        LoweredProgram program = ExpressionProgramLowerer.LowerLambda(expression, nameof(TestCompiler));

        LoweredReturnStatement returnStatement = Assert.IsType<LoweredReturnStatement>(program.Body.Statements[^1]);
        ValidatedExpression value = Assert.IsType<ValidatedExpression>(returnStatement.Value);
        Assert.Same(sum, value.Source);
        Assert.Same(local, Assert.Single(value.LocalReads));
    }

    [Fact]
    public void DefiniteAssignmentFindsReadsInNestedScopesAndLoops()
    {
        ParameterExpression condition = Expression.Parameter(typeof(bool), "condition");
        ParameterExpression nestedRead = Expression.Variable(typeof(int), "nestedRead");
        ParameterExpression loopRead = Expression.Variable(typeof(int), "loopRead");
        ParameterExpression inner = Expression.Variable(typeof(int), "inner");
        BlockExpression nestedScope = Expression.Block(
            [inner],
            Expression.Assign(inner, nestedRead),
            Expression.Assign(nestedRead, Expression.Constant(1)));
        LabelTarget breakTarget = Expression.Label();
        BlockExpression loopBody = Expression.Block(
            Expression.Assign(loopRead, Expression.Add(loopRead, Expression.Constant(1))),
            Expression.Empty());
        LoopExpression loop = Expression.Loop(
            Expression.IfThenElse(condition, loopBody, Expression.Break(breakTarget)),
            breakTarget);
        LambdaExpression expression = Expression.Lambda<Func<bool, int>>(
            Expression.Block([nestedRead, loopRead], nestedScope, loop, Expression.Constant(0)),
            condition);

        LoweredProgram program = ExpressionProgramLowerer.LowerLambda(expression, nameof(TestCompiler));
        ParameterExpression nestedLocal = Assert.Single(program.Body.Locals, local => ReferenceEquals(local, nestedRead));
        ParameterExpression loopLocal = Assert.Single(program.Body.Locals, local => ReferenceEquals(local, loopRead));

        Assert.IsType<LoweredBlock>(program.Body.Statements[0]);
        Assert.True(program.RequiresDefaultInitialization(nestedLocal));
        Assert.True(program.RequiresDefaultInitialization(loopLocal));
    }

    [Fact]
    public void LambdaLoweringFlattensVariableFreeTerminalBlock()
    {
        ParameterExpression input = Expression.Parameter(typeof(int), "input");
        ParameterExpression result = Expression.Variable(typeof(int), "result");
        BinaryExpression initialAssignment = Expression.Assign(result, Expression.Constant(0));
        BinaryExpression terminalAssignment = Expression.Assign(result, input);
        BlockExpression terminal = Expression.Block(terminalAssignment, result);
        LambdaExpression expression = Expression.Lambda<Func<int, int>>(
            Expression.Block([result], initialAssignment, terminal),
            input);

        LoweredProgram program = ExpressionProgramLowerer.LowerLambda(expression, nameof(TestCompiler));

        Assert.DoesNotContain(program.Body.Statements, statement => statement is LoweredBlock);
        Assert.Collection(
            program.Body.Statements,
            statement => Assert.Same(initialAssignment, Assert.IsType<LoweredAssignmentStatement>(statement).Operation.Source),
            statement => Assert.Same(terminalAssignment, Assert.IsType<LoweredAssignmentStatement>(statement).Operation.Source),
            statement => Assert.IsType<LoweredReturnStatement>(statement));
    }

    [Fact]
    public void StatementContextRejectsDiscardedValues()
    {
        TestCompiler compiler = CreateCompiler();
        BlockExpression expression = Expression.Block(Expression.Constant(1));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(expression));

        Assert.Contains("Only assignments, control flow, and explicit returns are valid in statement context.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValuedBreakOutsideLoopIsRejected()
    {
        TestCompiler compiler = CreateCompiler();
        LabelTarget target = Expression.Label(typeof(bool));
        GotoExpression expression = Expression.MakeGoto(GotoExpressionKind.Break, target, Expression.Constant(false), typeof(bool));
        BlockExpression statements = Expression.Block(expression);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(statements));

        Assert.Contains("Goto kind 'Break' is not valid in this context.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValueProducingLoopIsRejected()
    {
        TestCompiler compiler = CreateCompiler();
        LabelTarget breakTarget = Expression.Label(typeof(int));
        ConditionalExpression body = Expression.IfThenElse(
            Expression.Constant(false),
            Expression.Empty(),
            Expression.Break(breakTarget, Expression.Constant(0)));
        LoopExpression expression = Expression.Loop(body, breakTarget);
        BlockExpression statements = Expression.Block(expression);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(statements));

        Assert.Contains("Value-producing loops require explicit loop-result lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoopBreakMustTargetCurrentLoop()
    {
        TestCompiler compiler = CreateCompiler();
        LabelTarget loopBreak = Expression.Label();
        LabelTarget otherBreak = Expression.Label();
        ConditionalExpression body = Expression.IfThenElse(
            Expression.Constant(false),
            Expression.Empty(),
            Expression.Break(otherBreak));
        LoopExpression expression = Expression.Loop(body, loopBreak);
        BlockExpression statements = Expression.Block(expression);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(statements));

        Assert.Contains("does not match the supported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalReferenceOutsideDeclaringScopeIsRejected()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        BlockExpression expression = Expression.Block(
            Expression.Block([local], Expression.Assign(local, Expression.Constant(1))),
            Expression.Assign(local, Expression.Constant(2)));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(expression));

        Assert.Contains("outside its declaring scope", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExternalReferenceCannotBecomeLocalInLaterNestedBlock()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        BlockExpression expression = Expression.Block(
            Expression.Assign(local, Expression.Constant(1)),
            Expression.Block([local], Expression.Assign(local, Expression.Constant(2))));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(expression));

        Assert.Contains("outside its declaring scope", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LambdaFreeSymbolIsRejected()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression free = Expression.Parameter(typeof(int), "free");
        LambdaExpression expression = Expression.Lambda<Func<int>>(free);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(expression));

        Assert.Contains("Lambda symbol 'free' is neither an input nor a declared local", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SiblingScopesMayReuseLocalName()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression first = Expression.Variable(typeof(int), "difference");
        ParameterExpression second = Expression.Variable(typeof(int), "difference");
        BlockExpression expression = Expression.Block(
            Expression.IfThen(Expression.Constant(true), Expression.Block([first], Expression.Assign(first, Expression.Constant(1)))),
            Expression.IfThen(Expression.Constant(false), Expression.Block([second], Expression.Assign(second, Expression.Constant(2)))));

        string output = compiler.GetStatements(expression);

        Assert.Equal(2, output.Split("int difference;", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void LocalIdentityCannotBeDeclaredBySiblingScopes()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression local = Expression.Variable(typeof(int), "local");
        BlockExpression expression = Expression.Block(
            Expression.Block([local], Expression.Assign(local, Expression.Constant(1))),
            Expression.Block([local], Expression.Assign(local, Expression.Constant(2))));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(expression));

        Assert.Contains("declared by more than one scope", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnmodeledArithmeticNodesAreRejectedFromPrograms()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression input = Expression.Parameter(typeof(int), "input");
        LambdaExpression negate = Expression.Lambda<Func<int, int>>(Expression.Negate(input), input);
        LambdaExpression divide = Expression.Lambda<Func<int, int>>(Expression.Divide(input, Expression.Constant(2)), input);

        NotSupportedException negateException = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(negate));
        NotSupportedException divideException = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(divide));

        Assert.Contains("The expression is not part of the supported value-expression set.", negateException.Message, StringComparison.Ordinal);
        Assert.Contains("The expression is not part of the supported value-expression set.", divideException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnumConstantsAreRejectedUntilTargetSpecificLoweringExists()
    {
        TestCompiler compiler = CreateCompiler();
        ConstantExpression expression = Expression.Constant(TestEnum.Value);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetValue(expression));

        Assert.Contains("Enum constants require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnumDefaultsAreRejectedUntilTargetSpecificLoweringExists()
    {
        TestCompiler compiler = CreateCompiler();
        DefaultExpression expression = Expression.Default(typeof(TestEnum));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetValue(expression));

        Assert.Contains("Default values of type", exception.Message, StringComparison.Ordinal);
        Assert.Contains("require target-specific lowering", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UserDefinedBinaryOperatorsAreRejectedFromPrograms()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression left = Expression.Parameter(typeof(TestNumber), "left");
        ParameterExpression right = Expression.Parameter(typeof(TestNumber), "right");
        LambdaExpression expression = Expression.Lambda<Func<TestNumber, TestNumber, TestNumber>>(Expression.Add(left, right), left, right);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(expression));

        Assert.Contains("User-defined binary operators require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LiftedBinaryOperatorsAreRejectedFromFragments()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression left = Expression.Parameter(typeof(int?), "left");
        ParameterExpression right = Expression.Parameter(typeof(int?), "right");
        BinaryExpression expression = Expression.Add(left, right);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetValue(expression));

        Assert.Contains("Lifted binary operators require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UserDefinedUnaryOperatorsAreRejectedFromPrograms()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression value = Expression.Parameter(typeof(TestNumber), "value");
        LambdaExpression expression = Expression.Lambda<Func<TestNumber, TestNumber>>(Expression.Not(value), value);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetLambdaBody(expression));

        Assert.Contains("User-defined unary operators require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LiftedUnaryOperatorsAreRejectedFromFragments()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression value = Expression.Parameter(typeof(int?), "value");
        UnaryExpression expression = Expression.Not(value);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetValue(expression));

        Assert.Contains("Lifted unary operators require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UserDefinedCompoundOperatorsAreRejectedFromPrograms()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression local = Expression.Variable(typeof(TestNumber), "local");
        BlockExpression expression = Expression.Block(
            [local],
            Expression.AddAssign(local, Expression.Default(typeof(TestNumber))));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(expression));

        Assert.Contains("User-defined binary operators require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompoundOperatorConversionsAreRejectedFromPrograms()
    {
        TestCompiler compiler = CreateCompiler();
        ParameterExpression local = Expression.Variable(typeof(TestNumber), "local");
        ParameterExpression conversionValue = Expression.Parameter(typeof(long), "value");
        LambdaExpression conversion = Expression.Lambda<Func<long, TestNumber>>(Expression.Default(typeof(TestNumber)), conversionValue);
        BlockExpression expression = Expression.Block(
            [local],
            Expression.AddAssign(local, Expression.Constant(1), null, conversion));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => compiler.GetStatements(expression));

        Assert.Contains("Binary conversions require target-specific lowering.", exception.Message, StringComparison.Ordinal);
    }

    private static TestCompiler CreateCompiler() => new TestCompiler(CreateMap());

    private static TypeMap CreateMap()
    {
        ITypeDef[] defs =
        [
            new NullTypeDef("null"),
            new BooleanTypeDef("bool"),
            new StringTypeDef("string", QuoteString),
            new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "int.MinValue", "int.MaxValue"),
            new ObjectTypeDef((_, type) => type.Name, (_, value) => value.ToString() ?? string.Empty)
        ];

        return new TypeMap(defs, GeneratorEncoding.Utf8Bytes);
    }

    private static BlockExpression CreateVoidAssignment(ParameterExpression local, int value) =>
        Expression.Block(Expression.Assign(local, Expression.Constant(value)), Expression.Empty());

    private static bool TryAssign(out int value)
    {
        value = 1;
        return true;
    }

    private static string QuoteString(string value) => "\"" + value + "\"";

    private enum TestEnum
    {
        Value
    }

    private sealed class CapturedData(int value)
    {
        public int Value { get; } = value;
    }

    private readonly struct TestNumber
    {
        public static TestNumber operator +(TestNumber left, TestNumber _) => left;
        public static long operator +(TestNumber _, int right) => right;
        public static TestNumber operator ~(TestNumber value) => value;
    }

    private sealed class TestCompiler(TypeMap map) : ExpressionCompiler(map);
}