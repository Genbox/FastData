using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Execution;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Tests.Code;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Building.IrFactory;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LookupFoldTests
{
    [Theory]
    [InlineData(IrTextEncoding.Utf8)]
    [InlineData(IrTextEncoding.Utf16)]
    public void CustomFoldDrivesComparisonAndFragmentMatching(IrTextEncoding encoding)
    {
        IrLiteral Compare(string left, string right) => LookupTestPrograms.Evaluate(IrType.String, IrType.I32, x => x.Keys.Compare(Constant(left), Constant(right)), encoding, CreateDashFold);

        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.String, encoding, CreateDashFold);
        IrBlock body = Block(ReturnIf(builder.Text.EqualsAt(Read(builder.KeyParameterId), Constant(0), Constant("a_b"), builder.Keys.FoldFunctionId!.Value), Constant(0), Constant(-1)));
        FastDataProgram lookup = builder.Build(body);
        IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));

        Assert.Equal(Constant(0), Compare("A-B", "a_b"));
        Assert.Equal(Constant(-1), Compare("A-B", "a_c"));
        Assert.Equal(Constant(0), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("A-B")));
        Assert.Equal(Constant(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("A-C")));
    }

    private static IrFunctionId CreateDashFold(IrProgramBuilder program)
    {
        IrType unit = program.Text.CodeUnitType;
        IrFunctionId ascii = LookupText.CreateAsciiFold(program);
        IrFunctionBuilder function = program.Function("FoldDash", unit);
        IrVariableId value = function.Parameter("unit", unit);
        IrLiteral dash = program.Text.Unit('-');
        IrLiteral underscore = program.Text.Unit('_');
        function.Define(Block(ReturnIf(Equal(Read(value), dash), underscore, Call(ascii, Read(value)))));
        return function.Id;
    }
}