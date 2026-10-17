using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrConversionTests
{
    [Theory]
    [InlineData(IrTypeKind.I8, "-128", IrTypeKind.I64, "-128")]
    [InlineData(IrTypeKind.U8, "255", IrTypeKind.I16, "255")]
    [InlineData(IrTypeKind.I8, "-1", IrTypeKind.U64, "18446744073709551615")]
    [InlineData(IrTypeKind.U32, "511", IrTypeKind.U8, "255")]
    [InlineData(IrTypeKind.U16, "255", IrTypeKind.I8, "-1")]
    public void NumericConversionsWidenAndWrap(IrTypeKind source, string value, IrTypeKind target, string expected)
    {
        IrType type = IrType.Scalar(target);
        IrExpression conversion = Convert(type, VectorLiteral(IrType.Scalar(source), value));
        Assert.Equal(VectorLiteral(type, expected), Evaluate(type, conversion));
    }

    [Theory]
    [InlineData(IrTypeKind.F32, "7FA12345", IrTypeKind.U32, "2141266757")]
    [InlineData(IrTypeKind.F64, "8000000000000000", IrTypeKind.U64, "9223372036854775808")]
    public void BitCastsPreserveTheExactPayload(IrTypeKind source, string bits, IrTypeKind target, string expected)
    {
        IrType type = IrType.Scalar(source);
        IrType integerType = IrType.Scalar(target);
        IrLiteral literal = VectorLiteral(type, bits);
        IrExpression integer = new IrConversionExpression(integerType, ConversionKind.BitCast, literal);
        Assert.Equal(VectorLiteral(integerType, expected), Evaluate(integerType, integer));
        Assert.Equal(literal, Evaluate(type, new IrConversionExpression(type, ConversionKind.BitCast, integer)));
    }
}