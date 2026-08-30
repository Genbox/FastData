using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.Rust;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.TestHarness.Runner.Tests;

public sealed class HybleHashFilterRenderingTests
{
    [Fact]
    public void StringLookupsCompareFullStoredHashBeforeEqualityForEveryTarget()
    {
        string[] keys = ["alpha-0001", "bravo-0002", "charlie-003", "delta-0004", "echo-00005", "foxtrot-006"];
        int[] values = [11, 22, 33, 44, 55, 66];
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = StructureType.Hyble,
            StringAnalyzerConfig = null
        };
        config.EarlyExitConfig.Disabled = true;

        string csharp = FastDataGenerator.GenerateKeyed(keys, values, config, new CSharpCodeGenerator(new CSharpCodeGeneratorConfig("FastData"))).Source;
        string cpp = FastDataGenerator.GenerateKeyed(keys, values, config, new CPlusPlusCodeGenerator(new CPlusPlusCodeGeneratorConfig("fastdata"))).Source;
        string rust = FastDataGenerator.GenerateKeyed(keys, values, config, new RustCodeGenerator(new RustCodeGeneratorConfig("FastData"))).Source;

        Assert.Contains("internal ulong HashCode;", csharp, StringComparison.Ordinal);
        Assert.Contains("return hash == entry.HashCode &&", csharp, StringComparison.Ordinal);
        Assert.Contains("if (hash == entry.HashCode &&", csharp, StringComparison.Ordinal);
        Assert.Contains("uint64_t hash_code;", cpp, StringComparison.Ordinal);
        Assert.Contains("return entry.hash_code == hash &&", cpp, StringComparison.Ordinal);
        Assert.Contains("if (entry.hash_code == hash &&", cpp, StringComparison.Ordinal);
        Assert.Contains("hash_code: u64,", rust, StringComparison.Ordinal);
        Assert.Contains("entry.hash_code == hash &&", rust, StringComparison.Ordinal);
        Assert.Contains("if entry.hash_code == hash &&", rust, StringComparison.Ordinal);

        Func<string, bool> contains = CompilationHelper.GetDelegate<Func<string, bool>>(csharp, static types => types.Single(x => x.Name == "FastData"), static methods => methods.Single(x => x.Name == "Contains"), true);
        TryLookupDelegate tryLookup = CompilationHelper.GetDelegate<TryLookupDelegate>(csharp, static types => types.Single(x => x.Name == "FastData"), static methods => methods.Single(x => x.Name == "TryLookup"), true);

        for (int i = 0; i < keys.Length; i++)
        {
            Assert.True(contains(keys[i]));
            Assert.True(tryLookup(keys[i], out int value));
            Assert.Equal(values[i], value);
        }

        Assert.False(contains("alpha-0002"));
        Assert.False(tryLookup("alpha-0002", out _));
    }

    [Fact]
    public void NumericLookupsDoNotStoreHashFilterForEveryTarget()
    {
        int[] keys = [11, 22, 33, 44, 55, 66];
        NumericDataConfig config = new NumericDataConfig { StructureTypeOverride = StructureType.Hyble };
        config.EarlyExitConfig.Disabled = true;
        ICodeGenerator[] generators =
        [
            new CSharpCodeGenerator(new CSharpCodeGeneratorConfig("FastData")),
            new CPlusPlusCodeGenerator(new CPlusPlusCodeGeneratorConfig("fastdata")),
            new RustCodeGenerator(new RustCodeGeneratorConfig("FastData"))
        ];

        foreach (ICodeGenerator generator in generators)
        {
            string source = FastDataGenerator.Generate(keys, config, generator).Source;

            Assert.DoesNotContain("HashCode", source, StringComparison.Ordinal);
            Assert.DoesNotContain("hash_code", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CSharpIgnoreCaseUsesMatchingStoredAndQueryHashes()
    {
        string[] keys = ["ALPHA-0001", "Bravo-0002", "CHARLIE-003", "Delta-0004", "ECHO-00005", "Foxtrot-006"];
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = StructureType.Hyble,
            IgnoreCase = true,
            StringAnalyzerConfig = null
        };
        config.EarlyExitConfig.Disabled = true;

        string source = FastDataGenerator.Generate(keys, config, new CSharpCodeGenerator(new CSharpCodeGeneratorConfig("FastData"))).Source;
        Func<string, bool> contains = CompilationHelper.GetDelegate<Func<string, bool>>(source, static types => types.Single(x => x.Name == "FastData"), static methods => methods.Single(x => x.Name == "Contains"), true);

        Assert.True(contains("alpha-0001"));
        Assert.True(contains("BRAVO-0002"));
        Assert.True(contains("charlie-003"));
        Assert.False(contains("alpha-0002"));
    }

    private delegate bool TryLookupDelegate(string key, out int value);
}