using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Tests;

public class IrClrConverterTests
{
    public static TheoryData<object, IrLiteral> Scalars => new TheoryData<object, IrLiteral>
    {
        { true, new IrBoolLiteral(true) },
        { '\uD800', new IrCharLiteral(IrCharacterKind.Utf16CodeUnit, 0xD800) },
        { sbyte.MinValue, new IrI8Literal(sbyte.MinValue) },
        { byte.MaxValue, new IrU8Literal(byte.MaxValue) },
        { short.MinValue, new IrI16Literal(short.MinValue) },
        { ushort.MaxValue, new IrU16Literal(ushort.MaxValue) },
        { int.MinValue, new IrI32Literal(int.MinValue) },
        { uint.MaxValue, new IrU32Literal(uint.MaxValue) },
        { long.MinValue, new IrI64Literal(long.MinValue) },
        { ulong.MaxValue, new IrU64Literal(ulong.MaxValue) },
        { 1.25f, new IrF32Literal(0x3FA00000) },
        { -2.5d, new IrF64Literal(0xC004000000000000) },
        { "hello 😀", new IrStringLiteral("hello 😀") }
    };

    [Theory]
    [MemberData(nameof(Scalars))]
    public void ScalarCapturePreservesTypeAndValue(object value, IrLiteral expected)
    {
        Assert.Equal(expected, IrClrConverter.FromValue(value));
        Assert.Equal(expected.Type, IrClrConverter.FromClrType(value.GetType()));
    }

    [Fact]
    public void PrimitiveArrayCapturePreservesPayloads()
    {
        float[] source = [1.25f, -0.0f, BitConverter.Int32BitsToSingle(0x7FA12345)];
        IrPackedArrayLiteral<float> captured = Assert.IsType<IrPackedArrayLiteral<float>>(IrClrConverter.FromValue(source));
        Assert.Equal([0x3FA00000, int.MinValue, 0x7FA12345], captured.Values.Select(BitConverter.SingleToInt32Bits));
        Assert.Empty(Assert.IsType<IrPackedArrayLiteral<byte>>(IrClrConverter.FromValue(Array.Empty<byte>())).Values);
    }

    [Theory]
    [InlineData(IrTextEncoding.Utf8)]
    [InlineData(IrTextEncoding.Utf16)]
    public void StringArrayCapturePreservesPayloads(IrTextEncoding encoding)
    {
        IrPackedStringArrayLiteral strings = Assert.IsType<IrPackedStringArrayLiteral>(IrClrConverter.FromValue(new[] { "é😀", null }, encoding: encoding));
        Assert.Equal(encoding, strings.Encoding);
        Assert.Equal(new IrStringLiteral("é😀"), strings.ElementAt(0));
        Assert.Equal(new IrNullLiteral(IrType.String), strings.ElementAt(1));

        IrArrayLiteral nested = Assert.IsType<IrArrayLiteral>(IrClrConverter.FromValue(new[] { new[] { "é😀" } }, IrType.ArrayOf(IrType.ArrayOf(IrType.String)), encoding));
        IrPackedStringArrayLiteral inner = Assert.IsType<IrPackedStringArrayLiteral>(nested.Elements[0]);
        Assert.Equal(encoding, inner.Encoding);
        Assert.Equal(new IrStringLiteral("é😀"), inner.ElementAt(0));
    }
}