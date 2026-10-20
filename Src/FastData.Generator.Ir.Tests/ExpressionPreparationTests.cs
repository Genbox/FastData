using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Generator.Ir.Tests;

public class ExpressionPreparationTests
{
    [Theory]
    [InlineData(false, 32)]
    [InlineData(true, 0)]
    public void UnsignedMultiplicationCarriesItsWrappingWidth(bool explicitWrapping, int promotionWidth)
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.U16);
        IrExpression product = Multiply(Read(builder.KeyParameterId), new IrU16Literal(65535));
        FastDataProgram lookup = builder.Build(Block(Return(Convert(IrType.I32, product))));
        IrBackendCapabilities capabilities = new IrBackendCapabilities(GeneratorEncoding.Utf16CodeUnits, IrBuiltins.All)
        {
            ExplicitWrappingArithmetic = explicitWrapping,
            IntegerPromotionWidth = promotionWidth
        };
        PreparedLookup prepared = IrGeneratorPreparation.Prepare(lookup, capabilities);
        IrReturnStatement result = Assert.IsType<IrReturnStatement>(prepared.Functions[0].Body.Statements[0]);
        IrConversionExpression conversion = Assert.IsType<IrConversionExpression>(result.Value);
        PreparedWrappingArithmetic wrapping = Assert.IsType<PreparedWrappingArithmetic>(conversion.Operand);
        Assert.Equal(IrType.U16, wrapping.Type);
        Assert.Equal(BinaryOperation.Multiply, wrapping.Operation);
        Assert.Equal(builder.KeyParameterId, Assert.IsType<IrVariableExpression>(wrapping.Left).VariableId);
        Assert.Equal(new IrU16Literal(65535), wrapping.Right);
    }

    [Fact]
    public void NarrowArithmeticIsConvertedBeforeItsParentConsumesIt()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.U8);
        IrExpression sum = Add(Read(builder.KeyParameterId), new IrU8Literal(3));
        IrExpression product = Multiply(sum, new IrU8Literal(5));
        FastDataProgram lookup = builder.Build(Block(Return(Convert(IrType.I32, product))));
        IrBackendCapabilities capabilities = new IrBackendCapabilities(GeneratorEncoding.Utf16CodeUnits, IrBuiltins.All)
        {
            IntegerPromotionWidth = 32
        };
        PreparedLookup prepared = IrGeneratorPreparation.Prepare(lookup, capabilities);
        IrExpression result = Assert.IsType<IrReturnStatement>(prepared.Functions[0].Body.Statements[0]).Value;
        IrConversionExpression widened = Assert.IsType<IrConversionExpression>(result);
        Assert.Equal(IrType.I32, widened.Type);
        IrConversionExpression narrowedProduct = Assert.IsType<IrConversionExpression>(widened.Operand);
        Assert.Equal(IrType.U8, narrowedProduct.Type);
        IrBinaryExpression multiplication = Assert.IsType<IrBinaryExpression>(narrowedProduct.Operand);
        Assert.Equal(BinaryOperation.Multiply, multiplication.Operation);
        Assert.Equal(new IrU8Literal(5), multiplication.Right);
        IrConversionExpression narrowedSum = Assert.IsType<IrConversionExpression>(multiplication.Left);
        Assert.Equal(IrType.U8, narrowedSum.Type);
        IrBinaryExpression addition = Assert.IsType<IrBinaryExpression>(narrowedSum.Operand);
        Assert.Equal(BinaryOperation.Add, addition.Operation);
        Assert.Equal(builder.KeyParameterId, Assert.IsType<IrVariableExpression>(addition.Left).VariableId);
        Assert.Equal(new IrU8Literal(3), addition.Right);
    }

    [Fact]
    public void CallsAndLookupApiUsePreparedFunctionsAndStorage()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrFunctionBuilder helper = builder.Program.Function("FindSlot", IrType.I32);
        IrVariableId key = helper.Parameter("key", IrType.I32);
        helper.Define(Block(If(Equal(Read(key), I32(7)), Return(I32(0))), Return(I32(-1))));
        IrDataId values = builder.Program.AddData("values", builder.Program.Text.PackStrings(["seven"]));
        builder.Values = new LookupValues(values);
        FastDataProgram lookup = builder.Build(Block(Return(Call(helper.Id, Read(builder.KeyParameterId)))));
        PreparedLookup prepared = IrGeneratorPreparation.Prepare(lookup, new IrBackendCapabilities(GeneratorEncoding.Utf16CodeUnits, IrBuiltins.All));

        PreparedFunction find = prepared.Functions[0];
        PreparedFunctionCall call = Assert.IsType<PreparedFunctionCall>(Assert.IsType<IrReturnStatement>(find.Body.Statements[0]).Value);
        Assert.Equal(prepared.Functions[1].Name, call.Name);
        Assert.Equal(builder.KeyParameterId, Assert.IsType<IrVariableExpression>(Assert.Single(call.Arguments)).VariableId);
        PreparedFunctionCall apiFind = Assert.IsType<PreparedFunctionCall>(prepared.Api.Find);
        Assert.Equal(find.Name, apiFind.Name);
        Assert.Equal(new IrVariableId(0, IrType.I32), Assert.IsType<IrVariableExpression>(Assert.Single(apiFind.Arguments)).VariableId);
        IrBinaryExpression contains = Assert.IsType<IrBinaryExpression>(prepared.Api.Contains);
        Assert.Equal(BinaryOperation.GreaterThanOrEqual, contains.Operation);
        Assert.Same(apiFind, contains.Left);
        Assert.Equal(I32(0), contains.Right);
        Assert.Equal(IrType.String, prepared.Api.Value!.Type);
        IrIndexExpression value = Assert.IsType<IrIndexExpression>(prepared.Api.Value.Expression);
        Assert.Equal(values, Assert.IsType<IrDataExpression>(value.Collection).DataId);
        Assert.Equal(new IrVariableId(1, IrType.I32), Assert.IsType<IrVariableExpression>(value.Index).VariableId);
    }

    [Fact]
    public void SwitchPreparationPreservesLabelsResultsAndMiss()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrExpression key = Read(builder.KeyParameterId);
        FastDataProgram lookup = builder.Build(Block(If(Equal(key, I32(10)), Return(I32(0))),
            If(Equal(key, I32(20)), Return(I32(1))),
            If(Equal(key, I32(30)), Return(I32(2))),
            Return(I32(-1))));
        PreparedLookup prepared = IrGeneratorPreparation.Prepare(lookup, new IrBackendCapabilities(GeneratorEncoding.Utf16CodeUnits, IrBuiltins.All, IrSwitchKinds.Integral));
        IrBlock body = prepared.Functions[0].Body;
        PreparedSwitch dispatch = Assert.IsType<PreparedSwitch>(body.Statements[0]);
        Assert.Equal(builder.KeyParameterId, dispatch.Value.VariableId);
        Assert.Equal(new IrLiteral[] { I32(10), I32(20), I32(30) }, dispatch.Cases.Select(item => item.Label));
        Assert.Equal(new IrExpression[] { I32(0), I32(1), I32(2) }, dispatch.Cases.Select(item => item.Result));
        Assert.Equal(I32(-1), Assert.IsType<IrReturnStatement>(body.Statements[1]).Value);
    }
}