using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Execution;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Ir.Building.IrFactory;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LookupEnumerationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void ArrayCursorsEnumerateKeysAndValues(int count)
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrDataId keys = builder.Program.AddData("keys", new IrPackedArrayLiteral<int>(new[] { 10, 20, 30 }.Take(count).ToArray()));
        IrDataId values = builder.Program.AddData("values", new IrPackedArrayLiteral<bool>(new[] { true, false, true }.Take(count).ToArray()));

        // Next(-1) yields the first position, so an empty table returns -1 immediately.
        IrFunctionBuilder next = builder.Program.Function("Next", IrType.I64);
        IrVariableId position = next.Parameter("position", IrType.I64);
        IrExpression hasNext = Less(Read(position), Constant((long)count - 1));
        next.Define(Block(ReturnIf(hasNext, Add(Read(position), Constant(1L)), Constant(-1L))));

        IrFunctionBuilder keyAt = builder.Program.Function("KeyAt", IrType.I32);
        IrVariableId keyPosition = keyAt.Parameter("position", IrType.I64);
        keyAt.Define(Block(Return(Index(Read(keys), Convert(IrType.I32, Read(keyPosition))))));

        IrFunctionBuilder valueAt = builder.Program.Function("ValueAt", IrType.Bool);
        IrVariableId valuePosition = valueAt.Parameter("position", IrType.I64);
        valueAt.Define(Block(Return(Index(Read(values), Convert(IrType.I32, Read(valuePosition))))));
        builder.Values = new LookupValues(values);
        builder.Enumeration = new LookupEnumeration(next.Id, keyAt.Id, valueAt.Id);
        FastDataProgram program = builder.Build(Block(Return(Constant(-1))));
        IrInterpreter interpreter = new IrInterpreter(program.Program);

        Assert.Equal(new[] { 10, 20, 30 }.Take(count).Select(value => Constant(value)), program.Contract.Enumeration!.EnumerateKeys(interpreter));
        Assert.Equal(new[] { true, false, true }.Take(count).Select(value => Constant(value)), program.Contract.Enumeration!.EnumerateValues(interpreter));
    }

    [Fact]
    public void EnumerationEvaluatesOnlyRequestedElements()
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
        IrFunctionId read = builder.Program.ExternalFunction("read", new IrFunctionSignature(IrType.I32, [IrType.I64]));
        IrFunctionBuilder next = builder.Program.Function("Next", IrType.I64);
        IrVariableId position = next.Parameter("position", IrType.I64);
        next.Define(Block(Return(Add(Read(position), Constant(1L)))));
        IrFunctionBuilder element = builder.Program.Function("Element", IrType.I32);
        IrVariableId elementPosition = element.Parameter("position", IrType.I64);
        element.Define(Block(Return(Call(read, Read(elementPosition)))));
        builder.Enumeration = new LookupEnumeration(next.Id, element.Id);
        FastDataProgram program = builder.Build(Block(Return(Constant(-1))));
        int reads = 0;
        IrInterpreter interpreter = new IrInterpreter(program.Program, (_, arguments) =>
        {
            reads++;
            return Constant((int)((IrI64Literal)arguments[0]).Value);
        });
        IEnumerable<IrLiteral> enumeration = program.Contract.Enumeration!.EnumerateKeys(interpreter);
        Assert.Equal(0, reads);
        Assert.Equal([Constant(0), Constant(1), Constant(2)], enumeration.Take(3));
        Assert.Equal(3, reads);
    }
}