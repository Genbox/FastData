using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrPackedStringArrayTests
{
    [Theory]
    [InlineData(IrTextEncoding.Utf8)]
    [InlineData(IrTextEncoding.Utf16)]
    public void PackedStringsPreserveValuesAndEncodedStorage(IrTextEncoding encoding)
    {
        string?[] values = ["", null, "a\0z", "é😀", encoding == IrTextEncoding.Utf16 ? "\uD800" : "last"];
        IrPackedStringArrayLiteral packed = new IrText(encoding).PackStrings(values);
        IrDataId strings = new IrDataId(0);
        IrVariableId index = new IrVariableId(0);
        IrBlock body = Block(Return(Index(Read(strings), Read(index))));
        IrFunction function = new IrFunction("Read", IrType.String, [new IrVariable("index", IrType.I32)], [], body);
        IrProgram program = new IrProgram([function], [new IrData("strings", packed)], textEncoding: encoding);
        IrInterpreter interpreter = new IrInterpreter(program);

        for (int i = 0; i < values.Length; i++)
        {
            IrLiteral expected = values[i] is {} value ? new IrStringLiteral(value) : new IrNullLiteral(IrType.String);
            Assert.Equal(expected, interpreter.Invoke(new IrFunctionId(0), I32(i)));
            Assert.Equal(values[i] == null, packed.IsNull(i));
        }

        Assert.Equal(encoding == IrTextEncoding.Utf8 ? [0xC3, 0xA9, 0xF0, 0x9F, 0x98, 0x80] : [0xE9, 0, 0x3D, 0xD8, 0, 0xDE], packed.GetBytes(3).ToArray());
    }

    [Fact]
    public void EmptyAndNonNullArraysOmitTheNullBitmap()
    {
        IrPackedStringArrayLiteral empty = new IrText(IrTextEncoding.Utf16).PackStrings([]);
        Assert.Equal(0, empty.Count);
        Assert.Equal([0], empty.Offsets);
        Assert.Empty(empty.Data);
        Assert.Empty(empty.NullBitmap);
        IrPackedStringArrayLiteral strings = new IrText(IrTextEncoding.Utf16).PackStrings(["", "a"]);
        Assert.Empty(strings.NullBitmap);
        Assert.Equal(new IrStringLiteral(""), strings.ElementAt(0));
        Assert.Equal(new IrStringLiteral("a"), strings.ElementAt(1));
    }

    [Fact]
    public void StringArraysRequirePackedStorage()
    {
        IrArrayLiteral strings = new IrArrayLiteral(IrType.String, [new IrStringLiteral("value")]);
        Assert.Equal(IrDiagnosticCode.InvalidProgram, Assert.Throws<IrValidationException>(() => new IrProgram([], [new IrData("strings", strings)])).Code);
    }

    [Fact]
    public void Utf8StringBoundariesMustContainCompleteSequences()
    {
        IrPackedStringArrayLiteral packed = new IrPackedStringArrayLiteral(IrTextEncoding.Utf8, "é"u8.ToArray(), [0, 1, 2], []);
        Assert.Equal(IrDiagnosticCode.InvalidProgram, Assert.Throws<IrValidationException>(() => new IrProgram([], [new IrData("strings", packed)], textEncoding: IrTextEncoding.Utf8)).Code);
    }
}