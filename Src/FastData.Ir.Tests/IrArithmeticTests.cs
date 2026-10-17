using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrArithmeticTests
{
    [Theory]
    [InlineData(IrTypeKind.I32, BinaryOperation.Add, "-3", "7", "4")]
    [InlineData(IrTypeKind.I64, BinaryOperation.Subtract, "7", "10", "-3")]
    [InlineData(IrTypeKind.I16, BinaryOperation.Multiply, "-3", "7", "-21")]
    [InlineData(IrTypeKind.U8, BinaryOperation.Add, "255", "1", "0")]
    [InlineData(IrTypeKind.U32, BinaryOperation.Subtract, "0", "1", "4294967295")]
    [InlineData(IrTypeKind.U64, BinaryOperation.Multiply, "18446744073709551615", "2", "18446744073709551614")]
    [InlineData(IrTypeKind.U32, BinaryOperation.Divide, "17", "5", "3")]
    [InlineData(IrTypeKind.U32, BinaryOperation.Modulo, "17", "5", "2")]
    [InlineData(IrTypeKind.U8, BinaryOperation.And, "170", "15", "10")]
    [InlineData(IrTypeKind.U8, BinaryOperation.Or, "160", "15", "175")]
    [InlineData(IrTypeKind.U8, BinaryOperation.ExclusiveOr, "170", "255", "85")]
    [InlineData(IrTypeKind.U64, BinaryOperation.ShiftLeft, "1", "63", "9223372036854775808")]
    [InlineData(IrTypeKind.U32, BinaryOperation.ShiftRight, "100", "1", "50")]
    public void IntegralOperationsReturnExpectedValues(IrTypeKind kind, BinaryOperation operation, string left, string right, string expected)
    {
        IrType type = IrType.Scalar(kind);
        IrType rightType = operation is BinaryOperation.ShiftLeft or BinaryOperation.ShiftRight ? IrType.I32 : type;
        IrExpression expression = Binary(operation, VectorLiteral(type, left), VectorLiteral(rightType, right));
        Assert.Equal(VectorLiteral(type, expected), Evaluate(type, expression));
    }

    [Fact]
    public void NegationAndComplementInvertTheirValues()
    {
        Assert.Equal(Constant(false), Evaluate(IrType.Bool, Not(Constant(true))));
        Assert.Equal(Constant((byte)0x55), Evaluate(IrType.U8, Complement(IrType.U8, Constant((byte)0xAA))));
        Assert.Equal(Constant(-1), Evaluate(IrType.I32, Complement(IrType.I32, Constant(0))));
    }

    [Theory]
    [InlineData(LogicalOperation.AndAlso, false, true, false, 0)]
    [InlineData(LogicalOperation.AndAlso, true, false, false, 1)]
    [InlineData(LogicalOperation.OrElse, true, false, true, 0)]
    [InlineData(LogicalOperation.OrElse, false, true, true, 1)]
    public void LogicalOperationsEvaluateOnlyRequiredOperands(LogicalOperation operation, bool left, bool right, bool expected, int rightCalls)
    {
        IrProgramBuilder builder = new IrProgramBuilder();
        IrFunctionId first = builder.ExternalFunction("left", new IrFunctionSignature(IrType.Bool, []));
        IrFunctionId second = builder.ExternalFunction("right", new IrFunctionSignature(IrType.Bool, []));
        IrFunctionBuilder function = builder.Function("Evaluate", IrType.Bool);
        function.Define(Block(Return(new IrLogicalExpression(operation, Call(first), Call(second)))));

        int[] calls = new int[2];
        IrInterpreter interpreter = new IrInterpreter(builder.Build(), (external, _) =>
        {
            calls[external.Symbol == "left" ? 0 : 1]++;
            return Constant(external.Symbol == "left" ? left : right);
        });

        Assert.Equal(Constant(expected), interpreter.Invoke(function.Id));
        Assert.Equal([1, rightCalls], calls);
    }
}