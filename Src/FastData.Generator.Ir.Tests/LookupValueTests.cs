using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Model;
using static Genbox.FastData.Ir.Building.IrFactory;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LookupValueTests
{
    [Fact]
    public void MembershipOnlyLookupHasNoValueType()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        FastDataProgram lookup = builder.Build(Block(Return(Constant(-1))));

        Assert.Null(lookup.ValueType);
    }

    [Fact]
    public void ArrayStorageDerivesTheElementType()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrDataId values = builder.Program.AddData("values", builder.Program.Text.PackStrings(["seven"]));
        builder.Values = new LookupValues(values);
        FastDataProgram lookup = builder.Build(Block(Return(Constant(-1))));

        Assert.Equal(IrType.String, lookup.ValueType);
    }

    [Fact]
    public void RecordStorageDerivesTheFieldType()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrRecordId entry = builder.Program.DeclareRecord("Entry");
        IrFieldId field = builder.Program.AddField(entry, "value", IrType.String);
        IrDataId values = builder.Program.AddData("values", new IrArrayLiteral(IrType.Record(entry), [new IrRecordLiteral(entry, [Constant("seven")])]));
        builder.Values = new LookupValues(values, field);
        FastDataProgram lookup = builder.Build(Block(Return(Constant(-1))));

        Assert.Equal(IrType.String, lookup.ValueType);
    }
}