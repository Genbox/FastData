using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Tests;

public class StringOrderingTests
{
    [Theory]
    [InlineData(GeneratorEncoding.Utf8Bytes)]
    [InlineData(GeneratorEncoding.Utf16CodeUnits)]
    public void TargetOrderPreservesAssociatedValues(GeneratorEncoding encoding)
    {
        string[] keys = ["z", "\U0001F600", "\uE000", "a", "prefix\U0001F600", "prefix\uE000", "m"];
        int[] values = [11, 22, 33, 44, 55, 66, 77];
        CaptureGenerator generator = new CaptureGenerator(encoding);
        StringDataConfig config = new StringDataConfig { StructureTypeOverride = StructureType.BinarySearch };
        config.EarlyExitConfig.Disabled = true;
        FastDataGenerator.GenerateKeyed(keys, values, config, generator);
        string[] expectedKeys = encoding == GeneratorEncoding.Utf8Bytes
            ? ["a", "m", "prefix\uE000", "prefix\U0001F600", "z", "\uE000", "\U0001F600"]
            : ["a", "m", "prefix\U0001F600", "prefix\uE000", "z", "\U0001F600", "\uE000"];
        int[] expectedValues = encoding == GeneratorEncoding.Utf8Bytes ? [44, 77, 66, 55, 11, 33, 22] : [44, 77, 55, 66, 11, 22, 33];
        Assert.Equal(expectedKeys, generator.Context.Keys.ToArray());
        Assert.Equal(expectedValues, generator.Context.Values.ToArray());
    }

    [Theory]
    [InlineData(GeneratorEncoding.Utf8Bytes)]
    [InlineData(GeneratorEncoding.Utf16CodeUnits)]
    public void IgnoreCaseOrderPreservesAssociatedValues(GeneratorEncoding encoding)
    {
        string[] keys = ["A", "[", "_", "Z", "`", "a?"];
        int[] values = [11, 22, 33, 44, 55, 66];
        CaptureGenerator generator = new CaptureGenerator(encoding);
        StringDataConfig config = new StringDataConfig { StructureTypeOverride = StructureType.BinarySearch, IgnoreCase = true };
        config.EarlyExitConfig.Disabled = true;
        FastDataGenerator.GenerateKeyed(keys, values, config, generator);
        string[] expectedKeys = encoding == GeneratorEncoding.Utf8Bytes ? ["[", "_", "`", "A", "a?", "Z"] : ["A", "a?", "Z", "[", "_", "`"];
        int[] expectedValues = encoding == GeneratorEncoding.Utf8Bytes ? [22, 33, 55, 11, 66, 44] : [11, 66, 44, 22, 33, 55];
        Assert.Equal(expectedKeys, generator.Context.Keys.ToArray());
        Assert.Equal(expectedValues, generator.Context.Values.ToArray());
    }

    private sealed class CaptureGenerator(GeneratorEncoding encoding) : ICodeGenerator
    {
        public BinarySearchContext<string, int> Context { get; private set; } = null!;
        public GeneratorEncoding Encoding => encoding;

        public string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context)
        {
            Context = (BinarySearchContext<string, int>)context;
            return string.Empty;
        }
    }
}