using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Helpers;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
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
        IrExpression element = Index(Read(data), Read(x));
        function.Define(Block(Return(Bool(true))));
        IrProgram built = program.Build();
        IrFunction resolved = built.Resolve(function.Id);

        Assert.Equal(IrType.U16, IrExpressionTypes.Of(built, resolved, element));
        Assert.Equal(IrType.U16, IrExpressionTypes.Of(built, resolved, Add(element, element)));
        Assert.Equal(IrType.Bool, IrExpressionTypes.Of(built, resolved, Less(element, element)));
        Assert.Equal(IrType.I64, IrExpressionTypes.Of(built, resolved, Convert(IrType.I64, Read(x))));
    }
}