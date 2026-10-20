using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

/// <summary>One rejected example per validation rule that backends rely on.</summary>
public class IrValidationTests
{
    private static readonly IrVariableId _condition = new IrVariableId(0, IrType.Bool);
    private static readonly IrVariableId _value = new IrVariableId(1, IrType.I32);

    public static TheoryData<Func<IrExpression>> InvalidOperations => new TheoryData<Func<IrExpression>>
    {
        () => new IrBinaryExpression(BinaryOperation.Add, Constant(1f), Constant(2f)),
        () => new IrBinaryExpression(BinaryOperation.Modulo, Constant(5d), Constant(2d)),
        () => new IrBinaryExpression(BinaryOperation.Divide, Constant(7), Constant(2)),
        () => new IrBinaryExpression(BinaryOperation.Modulo, Constant(7), Constant(2)),
        () => new IrConversionExpression(IrType.F32, ConversionKind.Numeric, Constant(1L)),
        () => new IrConversionExpression(IrType.U32, ConversionKind.Numeric, Constant(1d)),
        () => new IrConversionExpression(IrType.U8, ConversionKind.BitCast, Constant((sbyte)-1)),
        () => new IrBinaryExpression(BinaryOperation.And, Constant(true), Constant(false)),
        () => new IrBinaryExpression(BinaryOperation.ShiftRight, Constant(8u), Constant(2u)),
        () => new IrBinaryExpression(BinaryOperation.ShiftLeft, Constant(1), Constant(2)),
        () => new IrBinaryExpression(BinaryOperation.Subtract, Constant(1), Constant(2L)),
        () => new IrBinaryExpression(BinaryOperation.Add, new IrCharLiteral(IrCharacterKind.Ascii, 0x61), new IrCharLiteral(IrCharacterKind.Ascii, 0x61)),
        () => new IrBinaryExpression(BinaryOperation.And, new IrCharLiteral(IrCharacterKind.Ascii, 0x61), new IrCharLiteral(IrCharacterKind.Ascii, 0x61)),
        () => new IrConversionExpression(IrType.U8, ConversionKind.BitCast, new IrCharLiteral(IrCharacterKind.Ascii, 0x61)),
        () => new IrConversionExpression(IrType.U16, ConversionKind.Numeric, new IrCharLiteral(IrCharacterKind.Ascii, 0x61))
    };

    [Theory]
    [MemberData(nameof(InvalidOperations))]
    public void OperandTypeRulesRejectInvalidOperations(Func<IrExpression> construct) => Assert.Equal(IrDiagnosticCode.TypeMismatch, Assert.Throws<IrValidationException>(() => construct()).Code);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf8ProgramsRejectUnpairedSurrogatesInLiterals(bool nested)
    {
        IrLiteral text = new IrStringLiteral("\uD800");
        IrProgramBuilder builder = new IrProgramBuilder(IrTextEncoding.Utf8);
        IrRecordId record = builder.DeclareRecord("Entry");
        builder.AddField(record, "text", IrType.String);
        IrLiteral value = nested ? new IrRecordLiteral(record, [text]) : text;
        builder.AddData("text", value);
        Assert.Equal(IrDiagnosticCode.InvalidProgram, Assert.Throws<IrValidationException>(() => builder.Build()).Code);
    }

    [Fact]
    public void FunctionsMustReturnOnEveryPath()
    {
        Check(IfElse(Read(_condition), Block(Return(true)), Block(Return(false))));
        Check(While(Constant(true)));
        Assert.Equal(IrDiagnosticCode.MissingReturn, Reject(If(Read(_condition), Return(true))));
        Assert.Equal(IrDiagnosticCode.MissingReturn, Reject(If(Constant(true), Return(true))));
    }

    [Fact]
    public void LocalsMustBeAssignedOnEveryPathBeforeTheyAreRead()
    {
        Check(IfElse(Read(_condition), Block(Assign(7)), Block(Assign(8))), ReturnValue());
        Check(While(Constant(true), Assign(7), new IrBreakStatement()), ReturnValue());
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(ReturnValue()));
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(If(Read(_condition), Assign(7)), ReturnValue()));
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(While(Read(_condition), Assign(7), new IrBreakStatement()), ReturnValue()));
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(new IrReturnStatement(AndAlso(Constant(false), Equal(Read(_value), Constant(7))))));
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(If(Constant(true), Assign(7)), ReturnValue()));
    }

    [Fact]
    public void UnreachableCodeStillRequiresValidBindingsTypesAndBreakPlacement()
    {
        Check(Return(true), ReturnValue());
        Assert.Equal(IrDiagnosticCode.TypeMismatch, Reject(Return(true), new IrReturnStatement(Constant(7))));
        Assert.Equal(IrDiagnosticCode.MissingDeclaration, Reject(Return(true), new IrReturnStatement(new IrVariableExpression(new IrVariableId(2, IrType.Bool)))));
        Assert.Equal(IrDiagnosticCode.MissingDeclaration, Reject(While(Constant(true)), new IrReturnStatement(new IrFunctionCallExpression(new IrFunctionId(1, IrType.Bool), []))));
        Assert.Equal(IrDiagnosticCode.InvalidProgram, Reject(Return(true), new IrBreakStatement()));
    }

    [Fact]
    public void OnlyContinuingBranchesContributeAssignments()
    {
        Check(IfElse(Read(_condition), Block(Return(false)), Block(Assign(7))), ReturnValue());
        Check(IfElse(Read(_condition), Block(While(Constant(true))), Block(Assign(7))), ReturnValue());
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(If(Read(_condition), Assign(7), Return(false)), ReturnValue()));
    }

    [Fact]
    public void LoopExitsIntersectAssignmentsWithoutLeakingNestedOrUnreachableWrites()
    {
        Check(While(Constant(true), If(Read(_condition), Assign(7), new IrBreakStatement()), While(Constant(true), new IrBreakStatement()), Assign(8), new IrBreakStatement()), ReturnValue());
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(While(Constant(true), If(Read(_condition), new IrBreakStatement()), Assign(7), new IrBreakStatement()), ReturnValue()));
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(While(Constant(true), If(Read(_condition), Assign(7), new IrBreakStatement()), new IrBreakStatement()), ReturnValue()));
        Assert.Equal(IrDiagnosticCode.UnassignedVariable, Reject(While(Constant(true), new IrBreakStatement(), Assign(7)), ReturnValue()));
    }

    [Fact]
    public void AggregateEqualityRequiresANullOperand()
    {
        IrProgramBuilder builder = new IrProgramBuilder();
        IrType type = IrType.ArrayOf(IrType.I32);
        IrFunctionBuilder function = builder.Function("Compare", IrType.Bool);
        IrVariableId left = function.Parameter("left", type);
        IrVariableId right = function.Parameter("right", type);
        Assert.Equal(IrDiagnosticCode.TypeMismatch, Assert.Throws<IrValidationException>(() => Equal(Read(left), Read(right))).Code);
    }

    [Fact]
    public void CallArgumentsMustMatchTheSignature()
    {
        IrProgramBuilder builder = new IrProgramBuilder();
        IrFunctionId external = builder.ExternalFunction("test.value", new IrFunctionSignature(IrType.I32, [IrType.I32]));
        builder.Function("Caller", IrType.I32).Define(Block(new IrReturnStatement(Call(external, Constant("wrong")))));
        Assert.Equal(IrDiagnosticCode.TypeMismatch, Assert.Throws<IrValidationException>(() => builder.Build()).Code);
    }

    private static IrReturnStatement Return(bool value) => new IrReturnStatement(Constant(value));
    private static IrAssignmentStatement Assign(int value) => new IrAssignmentStatement(_value, Constant(value));
    private static IrReturnStatement ReturnValue() => new IrReturnStatement(Equal(Read(_value), Constant(7)));

    private static IrProgram Check(params IrStatement[] statements) => new IrProgram([
        new IrFunction("Check", IrType.Bool, [new IrVariable("condition", IrType.Bool)], [new IrVariable("value", IrType.I32)], Block(statements))
    ]);

    private static IrDiagnosticCode Reject(params IrStatement[] statements) => Assert.Throws<IrValidationException>(() => Check(statements)).Code;
}