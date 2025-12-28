using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Tests;

public class FastDataGeneratorTests
{
    [Theory]
    [InlineData(DeduplicationMode.HashSetThrowOnDup)]
    [InlineData(DeduplicationMode.SortThrowOnDup)]
    public void Generate_ThrowOnDuplicate(DeduplicationMode mode)
    {
        FastDataConfig config = new FastDataConfig();
        config.DeduplicationMode = mode;

        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate(["item", "item"], config, new DummyGenerator()));
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate([1, 2, 2], config, new DummyGenerator()));
    }

    [Theory]
    [InlineData(DeduplicationMode.Disabled)]
    [InlineData(DeduplicationMode.HashSet)]
    [InlineData(DeduplicationMode.Sort)]
    public void Generate_NoThrowOnDuplicates(DeduplicationMode mode)
    {
        FastDataConfig config = new FastDataConfig();
        config.DeduplicationMode = mode;

        FastDataGenerator.Generate(["item", "item"], config, new DummyGenerator());
        FastDataGenerator.Generate([1, 2, 2], config, new DummyGenerator());
    }

    [Theory]
    [InlineData(DeduplicationMode.HashSet)]
    [InlineData(DeduplicationMode.Sort)]
    public void GenerateKeyed_StringDeduplication_RemovesDuplicates(DeduplicationMode mode)
    {
        string[] keys = ["b", "a", "b", "c"];
        string[] values = ["vb", "va", "vb", "vc"];

        FastDataConfig config = new FastDataConfig(StructureType.Array);
        config.DeduplicationMode = mode;

        ContextCaptureGenerator generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed(keys, values, config, generator);

        ArrayContext<string, string> ctx = Assert.IsType<ArrayContext<string, string>>(generator.Context);
        if (mode == DeduplicationMode.HashSet)
        {
            Assert.True(ctx.Keys.Span.SequenceEqual(["b", "a", "c"]));
            Assert.True(ctx.Values.Span.SequenceEqual(["vb", "va", "vc"]));
        }
        else
        {
            Assert.True(ctx.Keys.Span.SequenceEqual(["a", "b", "c"]));
            Assert.True(ctx.Values.Span.SequenceEqual(["va", "vb", "vc"]));
        }
    }

    [Theory]
    [InlineData(DeduplicationMode.HashSet)]
    [InlineData(DeduplicationMode.Sort)]
    public void GenerateKeyed_NumericDeduplication_RemovesDuplicates(DeduplicationMode mode)
    {
        int[] keys = [3, 1, 3, 2];
        string[] values = ["v3", "v1", "v3", "v2"];
        FastDataConfig config = new FastDataConfig(StructureType.Array);
        config.DeduplicationMode = mode;
        ContextCaptureGenerator generator = new ContextCaptureGenerator();

        FastDataGenerator.GenerateKeyed(keys, values, config, generator);

        ArrayContext<int, string> ctx = Assert.IsType<ArrayContext<int, string>>(generator.Context);
        if (mode == DeduplicationMode.HashSet)
        {
            Assert.True(ctx.Keys.Span.SequenceEqual([3, 1, 2]));
            Assert.True(ctx.Values.Span.SequenceEqual(["v3", "v1", "v2"]));
        }
        else
        {
            Assert.True(ctx.Keys.Span.SequenceEqual([1, 2, 3]));
            Assert.True(ctx.Values.Span.SequenceEqual(["v1", "v2", "v3"]));
        }
    }

    [Fact]
    public void Generate_ThrowOnInvalidType()
    {
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate([DateTime.Now, DateTime.UtcNow], new FastDataConfig(StructureType.Array), new DummyGenerator()));
    }

    [Fact]
    public void Generate_Overloads_String_Work()
    {
        string[] keys = ["a", "b", "c"];
        int[] values = [1, 2, 3];

        FastDataConfig config = new FastDataConfig(StructureType.Array);
        config.DeduplicationMode = DeduplicationMode.Disabled;

        ContextCaptureGenerator generator = new ContextCaptureGenerator();
        FastDataGenerator.Generate((ReadOnlyMemory<string>)keys, config, generator);
        AssertKeysOnly(generator, keys);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.Generate(keys, config, generator);
        AssertKeysOnly(generator, keys);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.Generate<string>((ReadOnlyMemory<string>)keys, config, generator);
        AssertKeysOnly(generator, keys);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed((ReadOnlyMemory<string>)keys, (ReadOnlyMemory<int>)values, config, generator);
        AssertKeysAndValues(generator, keys, values);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed(keys, values, config, generator);
        AssertKeysAndValues(generator, keys, values);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed<string, int>((ReadOnlyMemory<string>)keys, (ReadOnlyMemory<int>)values, config, generator);
        AssertKeysAndValues(generator, keys, values);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed(keys, values, config, generator);
        AssertKeysAndValues(generator, keys, values);
    }

    [Fact]
    public void Generate_Overloads_Int32_Work()
    {
        int[] keys = [1, 2, 3];
        string[] values = ["v1", "v2", "v3"];

        FastDataConfig config = new FastDataConfig(StructureType.Array);
        config.DeduplicationMode = DeduplicationMode.Disabled;

        ContextCaptureGenerator generator = new ContextCaptureGenerator();
        FastDataGenerator.Generate((ReadOnlyMemory<int>)keys, config, generator);
        AssertKeysOnly(generator, keys);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.Generate(keys, config, generator);
        AssertKeysOnly(generator, keys);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.Generate<int>((ReadOnlyMemory<int>)keys, config, generator);
        AssertKeysOnly(generator, keys);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed((ReadOnlyMemory<int>)keys, (ReadOnlyMemory<string>)values, config, generator);
        AssertKeysAndValues(generator, keys, values);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed(keys, values, config, generator);
        AssertKeysAndValues(generator, keys, values);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed<int, string>((ReadOnlyMemory<int>)keys, (ReadOnlyMemory<string>)values, config, generator);
        AssertKeysAndValues(generator, keys, values);

        generator = new ContextCaptureGenerator();
        FastDataGenerator.GenerateKeyed<int, string>(keys, values, config, generator);
        AssertKeysAndValues(generator, keys, values);
    }

    [Fact]
    public void GenerateKeyed_HashTablePerfect_ReordersValuesToMatchSlots()
    {
        int[] keys = [2, 0, 1];
        string[] values = ["v2", "v0", "v1"];
        FastDataConfig config = new FastDataConfig(StructureType.HashTable) { HashCapacityFactor = 1 };
        ContextCaptureGenerator generator = new ContextCaptureGenerator();

        FastDataGenerator.GenerateKeyed(keys, values, config, generator);

        HashTablePerfectContext<int, string> ctx = Assert.IsType<HashTablePerfectContext<int, string>>(generator.Context);
        ReadOnlySpan<string> ctxValues = ctx.Values.Span;

        Assert.False(ctxValues.IsEmpty);

        for (int i = 0; i < ctx.Data.Length; i++)
        {
            KeyValuePair<int, ulong> entry = ctx.Data[i];
            string value = ctxValues[i];
            Assert.Equal($"v{entry.Key}", value);
        }
    }

    [Fact]
    public void GenerateKeyed_HashTablePerfect_StoresHashCodeWhenCapacityFactorIsHigh()
    {
        int[] keys = [0, 1, 2];
        string[] values = ["v0", "v1", "v2"];
        FastDataConfig config = new FastDataConfig(StructureType.HashTable) { HashCapacityFactor = 2 };
        ContextCaptureGenerator generator = new ContextCaptureGenerator();

        FastDataGenerator.GenerateKeyed(keys, values, config, generator);

        HashTablePerfectContext<int, string> ctx = Assert.IsType<HashTablePerfectContext<int, string>>(generator.Context);
        Assert.True(ctx.StoreHashCode);
    }

    [Fact]
    public void Generate_Trimming_UsesSuffixFromEnd()
    {
        string[] keys = ["prefooSUF", "prebarSUF"];
        FastDataConfig config = new FastDataConfig(StructureType.Array) { EnablePrefixSuffixTrimming = true };
        TrimCaptureGenerator generator = new TrimCaptureGenerator();

        FastDataGenerator.Generate(keys, config, generator);

        Assert.Equal("pre", generator.TrimPrefix);
        Assert.Equal("SUF", generator.TrimSuffix);
    }

    // [Fact]
    // public async Task Generate_SpanSupport()
    // {
    //     ReadOnlySpan<int> span = [1, 2, 3, 4, 5];
    //     string source = FastDataGenerator.Generate(span, new FastDataConfig(StructureType.Array), new DummyGenerator());
    //
    //     await Verify(source)
    //           .UseFileName("SupportSpan")
    //           .UseDirectory("Features")
    //           .DisableDiff();
    // }

    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task Generate_CommonInputs(ITestData testData)
    {
        testData.Generate(_ => new DummyGenerator(), out GeneratorSpec spec);
        Assert.NotNull(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Verify")
              .DisableDiff();
    }

    public static TheoryData<ITestData> GetTestData()
    {
        TheoryData<ITestData> data = new TheoryData<ITestData>();
        foreach (StructureType type in Enum.GetValues<StructureType>())
        {
            data.Add(new TestData<string>(type, ["item1", "item2", "item3"]));
            data.Add(new TestData<int>(type, [int.MinValue, 0, int.MaxValue]));
            data.Add(new TestData<long>(type, [long.MinValue, 0, long.MaxValue]));
            data.Add(new TestData<double>(type, [double.MinValue, 0, double.MaxValue]));
        }
        return data;
    }

    private sealed class ContextCaptureGenerator : ICodeGenerator
    {
        public GeneratorEncoding Encoding => GeneratorEncoding.UTF8;

        public object? Context { get; private set; }

        public string Generate<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext<TValue> context)
        {
            Context = context;
            return string.Empty;
        }
    }

    private sealed class TrimCaptureGenerator : ICodeGenerator
    {
        public GeneratorEncoding Encoding => GeneratorEncoding.UTF8;

        public string TrimPrefix { get; private set; } = string.Empty;
        public string TrimSuffix { get; private set; } = string.Empty;

        public string Generate<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext<TValue> context)
        {
            TrimPrefix = genCfg.TrimPrefix;
            TrimSuffix = genCfg.TrimSuffix;
            return string.Empty;
        }
    }

    private static void AssertKeysOnly<TKey>(ContextCaptureGenerator generator, ReadOnlySpan<TKey> keys)
    {
        ArrayContext<TKey, byte> ctx = Assert.IsType<ArrayContext<TKey, byte>>(generator.Context);
        Assert.True(ctx.Values.IsEmpty);
        Assert.True(ctx.Keys.Span.SequenceEqual(keys));
    }

    private static void AssertKeysAndValues<TKey, TValue>(ContextCaptureGenerator generator, ReadOnlySpan<TKey> keys, ReadOnlySpan<TValue> values)
    {
        ArrayContext<TKey, TValue> ctx = Assert.IsType<ArrayContext<TKey, TValue>>(generator.Context);
        Assert.True(ctx.Keys.Span.SequenceEqual(keys));
        Assert.True(ctx.Values.Span.SequenceEqual(values));
    }
}
