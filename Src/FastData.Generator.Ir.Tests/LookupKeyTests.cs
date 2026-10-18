using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Tests.Code;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Building.IrFactory;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LookupKeyTests
{
    [Theory]
    [InlineData(IrTypeKind.I32, "7", 7UL)]
    [InlineData(IrTypeKind.I32, "-1", ulong.MaxValue)]
    [InlineData(IrTypeKind.F32, "00000000", 0UL)]
    [InlineData(IrTypeKind.F32, "80000000", 0UL)]
    [InlineData(IrTypeKind.F32, "3FC00000", 0x3FC00000UL)]
    [InlineData(IrTypeKind.F64, "0000000000000000", 0UL)]
    [InlineData(IrTypeKind.F64, "8000000000000000", 0UL)]
    [InlineData(IrTypeKind.F64, "BFF8000000000000", 0xBFF8000000000000UL)]
    public void NumericHashNormalizesSignedZeroAndPreservesOtherBits(IrTypeKind kind, string key, ulong expected)
    {
        IrType type = IrType.Scalar(kind);
        IrLiteral hash = LookupTestPrograms.Evaluate(type, IrType.U64, x => Call(x.Keys.AddNumericHashHelper(), VectorLiteral(type, key)));
        Assert.Equal(Constant(expected), hash);
    }

    [Theory]
    [InlineData(IrTypeKind.I32, "-2147483648", "2147483647", -1)]
    [InlineData(IrTypeKind.I32, "7", "7", 0)]
    [InlineData(IrTypeKind.I32, "2147483647", "-2147483648", 1)]
    [InlineData(IrTypeKind.F32, "00000000", "80000000", 0)]
    [InlineData(IrTypeKind.F32, "00000000", "3F800000", -1)]
    [InlineData(IrTypeKind.F64, "3FF0000000000000", "0000000000000000", 1)]
    [InlineData(IrTypeKind.F64, "8000000000000000", "0000000000000000", 0)]
    public void CompareOrdersKeys(IrTypeKind kind, string left, string right, int expected)
    {
        IrType type = IrType.Scalar(kind);
        IrLiteral order = LookupTestPrograms.Evaluate(type, IrType.I32, x => x.Keys.Compare(VectorLiteral(type, left), VectorLiteral(type, right)));
        Assert.Equal(Constant(expected), order);
    }

    [Theory]
    [InlineData(IrTypeKind.F32)]
    [InlineData(IrTypeKind.F64)]
    public void FloatingPointLookupFindsSignedZeroAndMissesNaN(IrTypeKind kind)
    {
        IrType type = IrType.Scalar(kind);
        FastDataProgramBuilder builder = new FastDataProgramBuilder(type);

        // CompareKeys orders NaN equal to every key, so only the NaN rejection that Build prepends makes NaN miss.
        IrBlock body = Block(ReturnIf(Equal(builder.Keys.Compare(Read(builder.KeyParameterId), Default(type)), Constant(0)), Constant(0), Constant(-1)));
        FastDataProgram lookup = builder.Build(body);
        IrInterpreter interpreter = new IrInterpreter(lookup.Program);
        IrLiteral negativeZero = kind == IrTypeKind.F32 ? new IrF32Literal(0x80000000) : new IrF64Literal(0x8000000000000000);
        IrLiteral nan = kind == IrTypeKind.F32 ? Constant(float.NaN) : Constant(double.NaN);
        IrLiteral one = kind == IrTypeKind.F32 ? Constant(1f) : Constant(1d);

        Assert.Equal(Constant(0), interpreter.Invoke(lookup.Contract.FindFunctionId, negativeZero));
        Assert.Equal(Constant(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, nan));
        Assert.Equal(Constant(-1), interpreter.Invoke(lookup.Contract.FindFunctionId, one));
    }

    [Theory]
    [InlineData(IrCharacterKind.Ascii)]
    [InlineData(IrCharacterKind.Utf16CodeUnit)]
    public void CharacterKeysHashAndCompareByCodeUnitValue(IrCharacterKind characterKind)
    {
        IrType type = IrType.Char(characterKind);
        IrCharLiteral low = new IrCharLiteral(characterKind, 0x41);
        IrCharLiteral high = new IrCharLiteral(characterKind, 0x7A);

        Assert.Equal(Constant(0x7AUL), LookupTestPrograms.Evaluate(type, IrType.U64, x => Call(x.Keys.AddNumericHashHelper(), high)));
        Assert.Equal(Constant(-1), LookupTestPrograms.Evaluate(type, IrType.I32, x => x.Keys.Compare(low, high)));
        Assert.Equal(Constant(0), LookupTestPrograms.Evaluate(type, IrType.I32, x => x.Keys.Compare(high, high)));
    }
}