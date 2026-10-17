using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Tests;

public class IrTextTests
{
    [Theory]
    [InlineData(IrTextEncoding.Utf8, "A\0Z", IrTypeKind.U8, new[] { 65, 0, 90 })]
    [InlineData(IrTextEncoding.Utf8, "é😀", IrTypeKind.U8, new[] { 0xc3, 0xa9, 0xf0, 0x9f, 0x98, 0x80 })]
    [InlineData(IrTextEncoding.Utf16, "é😀", IrTypeKind.U16, new[] { 0xe9, 0xd83d, 0xde00 })]
    public void LiteralEncodingAndLengthsUseTheSelectedUnits(IrTextEncoding encoding, string text, IrTypeKind unitKind, int[] expectedUnits)
    {
        IrProgram program = new IrProgramBuilder(encoding).Build();
        IrLiteral encoded = program.Text.EncodeLiteral(text);

        Assert.Equal(encoding, program.TextEncoding);
        Assert.Equal(IrType.Scalar(unitKind), program.Text.CodeUnitType);
        Assert.Equal(expectedUnits.Length, program.Text.GetLength(text));
        IEnumerable<int> units = encoding == IrTextEncoding.Utf8
            ? Assert.IsType<IrPackedArrayLiteral<byte>>(encoded).Values.Select(static unit => (int)unit)
            : Assert.IsType<IrPackedArrayLiteral<ushort>>(encoded).Values.Select(static unit => (int)unit);
        Assert.Equal(expectedUnits, units);
    }
}