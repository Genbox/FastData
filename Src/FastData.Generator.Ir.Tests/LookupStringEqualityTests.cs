using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Execution;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Tests.Code;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Building.IrFactory;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LookupStringEqualityTests
{
    [Theory]
    [InlineData(IrTextEncoding.Utf8, false)]
    [InlineData(IrTextEncoding.Utf16, false)]
    [InlineData(IrTextEncoding.Utf8, true)]
    [InlineData(IrTextEncoding.Utf16, true)]
    public void LookupUsesOrdinalOrAsciiFoldedKeys(IrTextEncoding encoding, bool fold)
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.String, encoding, fold ? LookupText.CreateAsciiFold : null);
        IrBlock body = Block(ReturnIf(builder.Keys.Equal(Read(builder.KeyParameterId), Constant("éA")), Constant(0), Constant(-1)));
        FastDataProgram lookup = builder.Build(body);
        IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));

        Assert.Equal(Constant(0), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("éA")));
        Assert.Equal(Constant(fold ? 0 : -1), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("éa")));
        Assert.Equal(Constant(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("ÉA")));
        Assert.Equal(Constant(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("")));
        Assert.Equal(Constant(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("é")));
        Assert.Equal(Constant(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, Constant("éAa")));
    }

    [Theory]
    [InlineData('@', '@')]
    [InlineData('A', 'a')]
    [InlineData('Z', 'z')]
    [InlineData('[', '[')]
    [InlineData('`', '`')]
    [InlineData('a', 'a')]
    [InlineData('{', '{')]
    public void AsciiFoldLowersOnlyTheUppercaseRange(char unit, char expected)
    {
        IrLiteral Fold(IrLiteral value, IrTextEncoding encoding) => LookupTestPrograms.Evaluate(IrType.String, value.Type, x => Call(x.Keys.FoldFunctionId!.Value, value), encoding, LookupText.CreateAsciiFold);

        Assert.Equal(new IrU8Literal((byte)expected), Fold(new IrU8Literal((byte)unit), IrTextEncoding.Utf8));
        Assert.Equal(new IrU16Literal(expected), Fold(new IrU16Literal(unit), IrTextEncoding.Utf16));
    }
}