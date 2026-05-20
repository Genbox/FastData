using Genbox.FastData.Config;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class GPerfEarlyExitTests
{
    [Fact]
    public void Generate_GPerfStringHash_AddsAsciiOnlyMandatoryExit()
    {
        string[] keys = ["a", "bb", "ccc", "dddd"];
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = typeof(HashTableStructure<,>),
            StringAnalyzerConfig = new StringAnalyzerConfig
            {
                BruteForceAnalyzerConfig = null,
                GeneticAnalyzerConfig = null,
                GPerfAnalyzerConfig = new GPerfAnalyzerConfig()
            }
        };

        CapturingGenerator generator = new CapturingGenerator();
        FastDataGenerator.Generate(keys, config, generator);

        StringGeneratorConfig generatorConfig = Assert.IsType<StringGeneratorConfig>(generator.Config);
        Assert.True((generatorConfig.GeneratorFunctions & GeneratorFunction.Length) != 0);
        Assert.True((generatorConfig.GeneratorFunctions & GeneratorFunction.IsAsciiOnly) != 0);
    }

    [Fact]
    public void Generate_Utf8GPerfStringHash_DoesNotAddAsciiOnlyMandatoryExit()
    {
        string[] keys = ["\u00e9", "abc", "\u65e5\u672c"];
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = typeof(HashTableStructure<,>),
            StringAnalyzerConfig = new StringAnalyzerConfig
            {
                BruteForceAnalyzerConfig = null,
                GeneticAnalyzerConfig = null,
                GPerfAnalyzerConfig = new GPerfAnalyzerConfig()
            }
        };

        CapturingGenerator generator = new CapturingGenerator(GeneratorEncoding.Utf8Bytes);
        FastDataGenerator.Generate(keys, config, generator);

        StringGeneratorConfig generatorConfig = Assert.IsType<StringGeneratorConfig>(generator.Config);
        Assert.False((generatorConfig.GeneratorFunctions & GeneratorFunction.IsAsciiOnly) != 0);
    }

    private sealed class CapturingGenerator(GeneratorEncoding encoding = GeneratorEncoding.AsciiBytes) : ICodeGenerator
    {
        public GeneratorConfigBase? Config { get; private set; }
        public GeneratorEncoding Encoding => encoding;

        public string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context)
        {
            Config = genCfg;
            return string.Empty;
        }
    }
}