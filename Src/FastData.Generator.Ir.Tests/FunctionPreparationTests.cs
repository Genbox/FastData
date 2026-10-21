using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Generator.Ir.Tests.Code;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Generator.Ir.Tests;

public class FunctionPreparationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuiltinCallsUseTheTargetsImplementation(bool native)
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.U64);
        FastDataProgram lookup = builder.Build(Block(Return(builder.Builtins.Call(IrBuiltinOperation.PopCount, Read(builder.KeyParameterId)))));
        PreparedLookup prepared = new PreparingGenerator(builtins: native ? IrBuiltins.All : IrBuiltins.Required).Prepare(lookup);
        PreparedFunction function = prepared.Functions[0];
        IrFunctionCallExpression call = Assert.IsType<IrFunctionCallExpression>(Assert.IsType<IrReturnStatement>(function.Function.Body.Statements[0]).Value);
        PreparedCall target = function.Calls[call.FunctionId.Value];

        if (native)
        {
            Assert.Single(prepared.Functions);
            Assert.Equal(IrBuiltinOperation.PopCount, target.Operation);
            Assert.Equal(IrBuiltinOperation.PopCount, prepared.RequiredBuiltins);
        }
        else
        {
            Assert.Equal(2, prepared.Functions.Count);
            Assert.Equal(prepared.Functions[1].Name, target.Name);
            Assert.Equal(IrBuiltinOperation.None, target.Operation);
            Assert.Equal(IrBuiltinOperation.None, prepared.RequiredBuiltins);
        }

        Assert.Same(lookup.Program.Resolve(builder.FindFunctionId), function.Function);
    }

    [Fact]
    public void ByteReadFallbackRequiresOnlyNativeUnitAccess()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.String);
        IrFunctionCallExpression read = builder.Builtins.Call(IrBuiltinOperation.ReadU32LittleEndian, Read(builder.KeyParameterId), I32(0));
        FastDataProgram lookup = builder.Build(Block(Return(Convert(IrType.I32, read))));
        PreparedLookup prepared = new PreparingGenerator(builtins: IrBuiltins.Required).Prepare(lookup);

        Assert.Equal(2, prepared.Functions.Count);
        Assert.Equal(prepared.Functions[1].Name, prepared.Functions[0].Calls[read.FunctionId.Value].Name);
        Assert.Equal(IrBuiltinOperation.TextUnitAt, prepared.RequiredBuiltins);
    }

    [Fact]
    public void UnsignedMultiplicationPreservesItsType()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.U16);
        IrBinaryExpression product = Multiply(Read(builder.KeyParameterId), new IrU16Literal(65535));
        FastDataProgram lookup = builder.Build(Block(Return(Convert(IrType.I32, product))));
        PreparedFunction function = new PreparingGenerator().Prepare(lookup).Functions[0];

        Assert.Equal(IrType.U16, product.Type);
        Assert.Same(lookup.Program.Resolve(builder.FindFunctionId), function.Function);
    }

    [Fact]
    public void NestedArithmeticPreservesEachOperationsType()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.U8);
        IrBinaryExpression sum = Add(Read(builder.KeyParameterId), new IrU8Literal(3));
        IrBinaryExpression product = Multiply(sum, new IrU8Literal(5));
        IrExpression widened = Convert(IrType.I32, product);
        FastDataProgram lookup = builder.Build(Block(Return(widened)));
        PreparedFunction function = new PreparingGenerator().Prepare(lookup).Functions[0];

        Assert.Same(widened, Assert.IsType<IrReturnStatement>(Assert.Single(function.Function.Body.Statements)).Value);
        Assert.Equal(IrType.U8, sum.Type);
        Assert.Equal(IrType.U8, product.Type);
        Assert.Equal(IrType.I32, widened.Type);
    }

    [Fact]
    public void PreparedFunctionsRetainBindingsAndRecordStorage()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrFunctionBuilder helper = builder.Program.Function("FindSlot", IrType.I32);
        IrVariableId key = helper.Parameter("key", IrType.I32);
        helper.Define(Block(If(Equal(Read(key), I32(7)), Return(I32(0))), Return(I32(-1))));
        IrRecordId entry = builder.Program.DeclareRecord("Entry");
        IrFieldId field = builder.Program.AddField(entry, "value", IrType.String);
        IrDataId values = builder.Program.AddData("values", new IrArrayLiteral(IrType.Record(entry), [new IrRecordLiteral(entry, [new IrStringLiteral("seven")])]));
        builder.Values = new LookupValues(values, field);
        FastDataProgram lookup = builder.Build(Block(Return(Call(helper.Id, Read(builder.KeyParameterId)))));
        PreparedLookup prepared = new PreparingGenerator().Prepare(lookup);

        PreparedFunction find = prepared.Functions[0];
        Assert.Equal(prepared.Functions[1].Name, find.Calls[helper.Id.Value].Name);
        Assert.Equal(find.Name, prepared.FunctionName(lookup.Contract.FindFunctionId));
        Assert.Same(lookup.Program.Resolve(entry), prepared.Records[entry.Value].Record);
        Assert.Equal(Assert.Single(prepared.Records[entry.Value].FieldNames), prepared.FieldName(field));
        Assert.Equal(IrType.String, prepared.Lookup.ValueType);
        Assert.Equal(values, prepared.Lookup.Contract.Values!.DataId);
        Assert.Equal(field, prepared.Lookup.Contract.Values.FieldId);
    }
}