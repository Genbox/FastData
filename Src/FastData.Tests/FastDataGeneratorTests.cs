using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Tests;

public class FastDataGeneratorTests
{
    [Fact]
    public void Generate_ThrowOnDuplicate()
    {
        FastDataConfig config = new FastDataConfig();
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate(["item", "item"], config, new DummyGenerator()));
    }

    [Fact]
    public void Generate_ThrowOnInvalidType()
    {
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate([DateTime.Now, DateTime.UtcNow], new FastDataConfig(StructureType.Array), new DummyGenerator()));
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
        Assert.NotNull(ctx.Values);

        for (int i = 0; i < ctx.Data.Length; i++)
        {
            KeyValuePair<int, ulong> entry = ctx.Data[i];
            string value = ctx.Values![i];
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
        FastDataConfig config = new FastDataConfig(StructureType.Array) { EnableTrimming = true };
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
}