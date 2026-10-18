using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;
using static Genbox.FastData.Ir.Building.IrFactory;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

/// <summary>One rejected example per lookup contract rule.</summary>
public class LookupValidationTests
{
    [Fact]
    public void FoldFunctionsMustMapCodeUnitsToCodeUnits()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.String, IrTextEncoding.Utf8, x =>
        {
            IrFunctionBuilder function = x.Function("Wrong", IrType.U16);
            function.Parameter("unit", IrType.U8);
            function.Define(Block(Return(new IrU16Literal(0))));
            return function.Id;
        });

        IrBlock body = Block(Return(Constant(-1)));
        Assert.Equal(IrDiagnosticCode.InvalidContract, Assert.Throws<IrValidationException>(() => builder.Build(body)).Code);
    }

    [Fact]
    public void ValueFieldsMustBelongToTheStoredRecord()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrRecordId stored = builder.Program.DeclareRecord("Stored");
        builder.Program.AddField(stored, "value", IrType.I32);
        IrRecordId other = builder.Program.DeclareRecord("Other");
        IrFieldId field = builder.Program.AddField(other, "value", IrType.I32);
        IrDataId values = builder.Program.AddData("values", new IrArrayLiteral(IrType.Record(stored), [new IrRecordLiteral(stored, [Constant(42)])]));
        builder.Values = new LookupValues(values, field);
        IrBlock body = Block(Return(Constant(0)));
        Assert.Equal(IrDiagnosticCode.InvalidContract, Assert.Throws<IrValidationException>(() => builder.Build(body)).Code);
    }

    [Fact]
    public void BuiltinSymbolsMustUseTheirCatalogSignature()
    {
        IrExternalFunction wrong = new IrExternalFunction(IrBuiltins.GetSymbol(IrBuiltinOperation.TextLength), new IrFunctionSignature(IrType.Bool, [IrType.String]));
        Assert.Equal(IrDiagnosticCode.TypeMismatch, Assert.Throws<IrValidationException>(() => IrBuiltins.Validate(new IrProgram([wrong]))).Code);
    }

    [Fact]
    public void UnrecognizedSymbolsAreAcceptedAsExternalFunctions()
    {
        IrExternalFunction external = new IrExternalFunction("fastdata.lookup.unknown", new IrFunctionSignature(IrType.I32, []));
        IrBuiltins.Validate(new IrProgram([external]));
    }

    [Fact]
    public void ValueEnumerationRequiresValueStorage()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrFunctionBuilder next = builder.Program.Function("Next", IrType.I64);
        next.Parameter("position", IrType.I64);
        next.Define(Block(Return(Constant(-1L))));
        IrFunctionBuilder element = builder.Program.Function("Element", IrType.I32);
        element.Parameter("position", IrType.I64);
        element.Define(Block(Return(Constant(0))));
        builder.Enumeration = new LookupEnumeration(next.Id, element.Id, element.Id);
        Assert.Equal(IrDiagnosticCode.InvalidContract, Assert.Throws<IrValidationException>(() => builder.Build(Block(Return(Constant(-1))))).Code);
    }
}