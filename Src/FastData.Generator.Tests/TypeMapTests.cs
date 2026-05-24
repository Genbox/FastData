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
    [InlineData(sbyte.MinValue - 1L, "short")]
    [InlineData(sbyte.MinValue, "sbyte")]
    [InlineData(sbyte.MaxValue, "sbyte")]
    [InlineData(short.MinValue - 1L, "int")]
    [InlineData(short.MinValue, "short")]
    [InlineData(short.MaxValue, "short")]
    [InlineData(int.MinValue - 1L, "long")]
    [InlineData(int.MinValue, "int")]
    [InlineData(int.MaxValue, "int")]
    public void GetSmallestIntTypeRespectsSignedLowerBounds(long value, string expected)
    {
        TypeMap map = CreateIntegerTypeMap();

        Assert.Equal(expected, map.GetSmallestIntType(value));
    }

    private static string Identity(string value) => value;

    private static TypeMap CreateIntegerTypeMap()
    {
        ITypeDef[] defs =
        [
            new IntegerTypeDef<sbyte>("sbyte", sbyte.MinValue, sbyte.MaxValue, "sbyte.MinValue", "sbyte.MaxValue"),
            new IntegerTypeDef<short>("short", short.MinValue, short.MaxValue, "short.MinValue", "short.MaxValue"),
            new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "int.MinValue", "int.MaxValue"),
            new IntegerTypeDef<long>("long", long.MinValue, long.MaxValue, "long.MinValue", "long.MaxValue")
        ];

        return new TypeMap(defs, GeneratorEncoding.Utf8Bytes);
    }

    private sealed class CustomObject;
}