using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.Tests;

public class TypeMapTests
{
    [Fact]
    public void DuplicateTypeSpecsThrow()
    {
        List<ITypeDef> defs =
        [
            new StringTypeDef("string", Identity),
            new StringTypeDef("other", Identity)
        ];

        Assert.Throws<InvalidOperationException>(() => new TypeMap(defs, GeneratorEncoding.Utf8Bytes));
    }

    [Fact]
    public void GetNullReturnsNullLabel()
    {
        ITypeDef[] defs =
        [
            new NullTypeDef("null"),
            new StringTypeDef("string", Identity)
        ];

        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Equal("null", map.GetNull());
    }

    [Fact]
    public void DynamicStringTypeDefResolvesByEncoding()
    {
        DynamicStringTypeDef dynamic = new DynamicStringTypeDef(
            new StringType(GeneratorEncoding.AsciiBytes, "ascii", Identity),
            new StringType(GeneratorEncoding.Utf8Bytes, "utf8", Identity));

        ITypeDef[] defs = [dynamic];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Equal("utf8", map.Get<string>().Name);
    }

    [Fact]
    public void GetTypeNameUsesRuntimeTypeNameForObject()
    {
        ObjectTypeDef objectDef = new ObjectTypeDef((_, type) => type.Name, (_, value) => value.ToString() ?? string.Empty);
        ITypeDef[] defs = [objectDef];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Equal(nameof(CustomObject), map.GetTypeName(typeof(CustomObject)));
    }

    [Fact]
    public void GetThrowsWhenTypeIsMissing()
    {
        ITypeDef[] defs = [new StringTypeDef("string", Identity)];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Throws<InvalidOperationException>(map.Get<int>);
    }

    [Theory]
    [InlineData(-129L, "short")]
    [InlineData(sbyte.MinValue, "sbyte")]
    [InlineData(sbyte.MaxValue, "sbyte")]
    [InlineData(short.MinValue - 1L, "int")]
    [InlineData(short.MinValue, "short")]
    [InlineData(short.MaxValue, "short")]
    [InlineData(int.MinValue - 1L, "long")]
    [InlineData(int.MinValue, "int")]
    [InlineData(int.MaxValue, "int")]
    public void GetSmallestIntTypeRespectsBounds(long value, string expected)
    {
        TypeMap map = CreateIntegerTypeMap();

        Assert.Equal(expected, map.GetSmallestSignedTypeName(value));
    }

    [Theory]
    [InlineData(0UL, typeof(byte))]
    [InlineData(byte.MaxValue, typeof(byte))]
    [InlineData(byte.MaxValue + 1UL, typeof(ushort))]
    [InlineData(ushort.MaxValue, typeof(ushort))]
    [InlineData(ushort.MaxValue + 1UL, typeof(uint))]
    [InlineData(uint.MaxValue, typeof(uint))]
    [InlineData(uint.MaxValue + 1UL, typeof(ulong))]
    [InlineData(ulong.MaxValue, typeof(ulong))]
    public void GetSmallestUIntStorageTypeRespectsBoundaries(ulong maxValue, Type expected)
    {
        TypeMap map = CreateIntegerTypeMap();

        Assert.Equal(expected, map.GetSmallestUnsignedType(maxValue));
    }

    [Theory]
    [InlineData(sbyte.MinValue, sbyte.MaxValue, typeof(sbyte))]
    [InlineData(-129L, sbyte.MaxValue, typeof(short))]
    [InlineData(sbyte.MinValue, 128L, typeof(short))]
    [InlineData(short.MinValue, short.MaxValue, typeof(short))]
    [InlineData(short.MinValue - 1L, short.MaxValue, typeof(int))]
    [InlineData(short.MinValue, short.MaxValue + 1L, typeof(int))]
    [InlineData(int.MinValue, int.MaxValue, typeof(int))]
    [InlineData(int.MinValue - 1L, int.MaxValue, typeof(long))]
    [InlineData(int.MinValue, int.MaxValue + 1L, typeof(long))]
    public void GetSmallestIntStorageTypeCoversSignedInterval(long minValue, long maxValue, Type expected)
    {
        TypeMap map = CreateIntegerTypeMap();

        Assert.Equal(expected, map.GetSmallestSignedType(minValue, maxValue));
    }

    [Fact]
    public void GetSmallestIntStorageTypeRejectsReversedInterval()
    {
        TypeMap map = CreateIntegerTypeMap();

        Assert.Throws<ArgumentException>(() => map.GetSmallestSignedType(1, 0));
    }

    private static string Identity(string value) => value;

    private static TypeMap CreateIntegerTypeMap()
    {
        ITypeDef[] defs =
        [
            new IntegerTypeDef<sbyte>("sbyte", sbyte.MinValue, sbyte.MaxValue, "sbyte.MinValue", "sbyte.MaxValue"),
            new IntegerTypeDef<byte>("byte", byte.MinValue, byte.MaxValue, "byte.MinValue", "byte.MaxValue"),
            new IntegerTypeDef<short>("short", short.MinValue, short.MaxValue, "short.MinValue", "short.MaxValue"),
            new IntegerTypeDef<ushort>("ushort", ushort.MinValue, ushort.MaxValue, "ushort.MinValue", "ushort.MaxValue"),
            new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "int.MinValue", "int.MaxValue"),
            new IntegerTypeDef<uint>("uint", uint.MinValue, uint.MaxValue, "uint.MinValue", "uint.MaxValue"),
            new IntegerTypeDef<long>("long", long.MinValue, long.MaxValue, "long.MinValue", "long.MaxValue"),
            new IntegerTypeDef<ulong>("ulong", ulong.MinValue, ulong.MaxValue, "ulong.MinValue", "ulong.MaxValue")
        ];

        return new TypeMap(defs, GeneratorEncoding.Utf8Bytes);
    }

    private sealed class CustomObject;
}