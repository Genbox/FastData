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
            new StringTypeDef("string", Identity),
            new StringTypeDef("other", Identity)
        ];

        Assert.Throws<InvalidOperationException>(() => new TypeMap(defs, GeneratorEncoding.Utf8Bytes));
    }

    [Fact]
    public void GetValueLiteralReturnsRegisteredNullLiteral()
    {
        ITypeDef[] defs = [new NullTypeDef("nil")];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Equal("nil", map.GetValueLiteral(null));
    }

    [Fact]
    public void GetValueLiteralThrowsWhenNullDefinitionIsMissing()
    {
        TypeMap map = new TypeMap([], GeneratorEncoding.Utf8Bytes);

        Assert.Throws<InvalidOperationException>(() => map.GetValueLiteral(null));
    }

    [Fact]
    public void GetValueLiteralDispatchesUsingBoxedRuntimeTypeWithoutObjectDefinition()
    {
        ITypeDef[] defs = [new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "min", "max", value => $"int:{value}")];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);
        object value = 42;

        Assert.Equal("int:42", map.GetValueLiteral(value));
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
    public void GetObjectDeclarationUsesObjectDefinition()
    {
        ObjectTypeDef objectDef = new ObjectTypeDef((_, type) => $"object {type.Name}", (_, value) => value.ToString() ?? string.Empty);
        ITypeDef[] defs = [objectDef];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Equal("object CustomObject", map.GetObjectDeclaration(typeof(CustomObject)));
    }

    [Fact]
    public void GetObjectDeclarationRejectsPrimitiveType()
    {
        TypeMap map = new TypeMap([], GeneratorEncoding.Utf8Bytes);

        Assert.Throws<ArgumentException>(() => map.GetObjectDeclaration(typeof(int)));
    }

    [Fact]
    public void GetObjectDeclarationThrowsWhenDefinitionLacksCapability()
    {
        ITypeDef[] defs = [new ValueOnlyObjectTypeDef()];
        TypeMap map = new TypeMap(defs, GeneratorEncoding.Utf8Bytes);

        Assert.Throws<InvalidOperationException>(() => map.GetObjectDeclaration(typeof(CustomObject)));
    }

    [Fact]
    public void GetObjectDeclarationThrowsWhenDefinitionIsMissing()
    {
        TypeMap map = new TypeMap([], GeneratorEncoding.Utf8Bytes);

        Assert.Throws<InvalidOperationException>(() => map.GetObjectDeclaration(typeof(CustomObject)));
    }

    [Fact]
    public void GetValueLiteralThrowsWhenRuntimeTypeIsMissing()
    {
        TypeMap map = new TypeMap([], GeneratorEncoding.Utf8Bytes);

        Assert.Throws<InvalidOperationException>(() => map.GetValueLiteral(42));
    }

    [Theory]
    [MemberData(nameof(GetUnsupportedValues))]
    public void GetValueLiteralRejectsUnsupportedRuntimeTypes(object value)
    {
        ObjectTypeDef objectDef = new ObjectTypeDef((_, type) => type.Name, (_, item) => item.ToString() ?? string.Empty);
        IntegerTypeDef<int> integerDef = new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "min", "max");
        TypeMap map = new TypeMap([objectDef, integerDef], GeneratorEncoding.Utf8Bytes);

        Assert.Throws<NotSupportedException>(() => map.GetValueLiteral(value));
    }

    [Theory]
    [InlineData(typeof(nint))]
    [InlineData(typeof(nuint))]
    public void GetObjectDeclarationRejectsNativeIntegerTypes(Type type)
    {
        ObjectTypeDef objectDef = new ObjectTypeDef((_, valueType) => valueType.Name, (_, value) => value.ToString() ?? string.Empty);
        TypeMap map = new TypeMap([objectDef], GeneratorEncoding.Utf8Bytes);

        Assert.Throws<NotSupportedException>(() => map.GetObjectDeclaration(type));
    }

    public static TheoryData<object> GetUnsupportedValues() =>
    [
        SampleEnum.Value,
        (nint)1,
        (nuint)1
    ];

    private static string Identity(string value) => value;

    private static class CustomObject;

    private enum SampleEnum
    {
        Value
    }

    private sealed class ValueOnlyObjectTypeDef : ITypeDef
    {
        public TypeCode KeyType => TypeCode.Object;
        public string Name => "object";
        public Func<TypeMap, object, string> PrintObj => (_, value) => value.ToString() ?? string.Empty;
    }
}