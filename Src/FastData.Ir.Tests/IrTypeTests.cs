using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrTypeTests
{
    [Fact]
    public void TypesCompareByStructureAndRecordIdentity()
    {
        IrType first = IrType.ArrayOf(IrType.Record(new IrRecordId(0)));
        IrType equal = IrType.ArrayOf(IrType.Record(new IrRecordId(0)));
        IrType different = IrType.ArrayOf(IrType.Record(new IrRecordId(1)));
        Dictionary<IrType, string> names = new Dictionary<IrType, string>(1) { [first] = "entries" };

        Assert.Equal("entries", names[equal]);
        Assert.Equal(first, equal);
        Assert.NotEqual(first, different);
        Assert.Equal(IrType.ArrayOf(IrType.I32), IrType.ArrayOf(IrType.I32));
        Assert.NotEqual(IrType.ArrayOf(IrType.I32), IrType.ArrayOf(IrType.U32));
    }

    [Theory]
    [InlineData(IrTypeKind.I8, IrTypeKind.U8, true)]
    [InlineData(IrTypeKind.U16, IrTypeKind.U16, false)]
    [InlineData(IrTypeKind.I32, IrTypeKind.U32, true)]
    [InlineData(IrTypeKind.I64, IrTypeKind.U64, true)]
    public void UnsignedOfKeepsTheWidth(IrTypeKind kind, IrTypeKind expected, bool signed)
    {
        IrType type = IrType.Scalar(kind);
        Assert.Equal(IrType.Scalar(expected), IrType.UnsignedOf(type));
        Assert.Equal(signed, type.IsSignedIntegral);
        Assert.Equal(!signed, type.IsUnsignedIntegral);
    }

    [Fact]
    public void FloatingPointTypesAreNotIntegral()
    {
        Assert.False(IrType.F64.IsSignedIntegral);
        Assert.False(IrType.F64.IsUnsignedIntegral);
    }

    [Fact]
    public void ExpressionTypesFollowTheOperatorRules()
    {
        IrProgramBuilder program = new IrProgramBuilder();
        IrDataId data = program.AddData("values", new IrPackedArrayLiteral<ushort>([1, 2]));
        IrFunctionBuilder function = program.Function("F", IrType.Bool);
        IrVariableId x = function.Parameter("x", IrType.I32);
        IrIndexExpression element = Index(Read(data), Read(x));
        IrRecordId entry = program.DeclareRecord("Entry");
        IrFieldId field = program.AddField(entry, "text", IrType.String);
        IrVariableId record = function.Parameter("entry", IrType.Record(entry));
        IrFunctionBuilder helper = program.Function("Text", IrType.String);
        IrFunctionCallExpression call = Call(helper.Id);
        IrFieldExpression text = Field(Read(record), field);
        function.Define(Block(Return(Bool(true))));
        helper.Define(Block(Return(new IrStringLiteral("hello"))));
        program.Build();

        Assert.Equal(IrType.I32, Read(x).Type);
        Assert.Equal(IrType.ArrayOf(IrType.U16), Read(data).Type);
        Assert.Equal(IrType.String, text.Type);
        Assert.Equal(IrType.String, call.Type);
        Assert.Equal(IrType.Bool, AndAlso(Equal(text, call), Bool(true)).Type);
        Assert.Equal(IrType.U16, element.Type);
        Assert.Equal(IrType.U16, Add(element, element).Type);
        Assert.Equal(IrType.Bool, Less(element, element).Type);
        Assert.Equal(IrType.I64, Convert(IrType.I64, Read(x)).Type);
    }
}