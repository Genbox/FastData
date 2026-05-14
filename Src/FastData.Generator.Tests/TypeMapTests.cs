using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;

namespace Genbox.FastData.Generator.Tests;

public class TypeMapTests
{
    [Fact]
    public void DuplicateTypeSpecsThrow()
    {
        List<ITypeDef> defs =
        [
            new StringTypeDef("string"),
            new StringTypeDef("other")
        ];

        Assert.Throws<InvalidOperationException>(() => new TypeMap(defs, GeneratorEncoding.Utf8Bytes));
    }

    [Fact]
    public void GetNullReturnsNullLabel()
    {
        ITypeDef[] defs =
        [
            new NullTypeDef("null"),
            new StringTypeDef("string")
        ];

        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Equal("null", map.GetNull());
    }

    [Fact]
    public void DynamicStringTypeDefResolvesByEncoding()
    {
        DynamicStringTypeDef dynamic = new DynamicStringTypeDef(
            new StringType(GeneratorEncoding.AsciiBytes, "ascii"),
            new StringType(GeneratorEncoding.Utf8Bytes, "utf8"));

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
        ITypeDef[] defs = [new StringTypeDef("string")];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Throws<InvalidOperationException>(map.Get<int>);
    }

    private sealed class CustomObject;
}