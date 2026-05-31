using Genbox.FastData.Config;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Structures;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class StringHashTestBase
{
    protected abstract string HarnessName { get; }
    protected abstract ICodeGenerator Generator { get; }

    [Fact]
    public async Task DefaultStringHashSource()
    {
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = typeof(HashTableStructure<,>),
            StringAnalyzerConfig = null
        };

        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["test1", "test2"];
        string source = FastDataGenerator.Generate(keys, config, Generator);

        await VerifyStringHashAsync(HarnessName, nameof(DefaultStringHashSource), source);
    }

    [Fact]
    public async Task BruteForceStringHashSource()
    {
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = typeof(HashTableStructure<,>),
            StringAnalyzerConfig = new StringAnalyzerConfig
            {
                BruteForceAnalyzerConfig = new BruteForceAnalyzerConfig(),
                GeneticAnalyzerConfig = null,
                GPerfAnalyzerConfig = null
            }
        };

        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["test1", "test2"];
        string source = FastDataGenerator.Generate(keys, config, Generator);

        await VerifyStringHashAsync(HarnessName, nameof(BruteForceStringHashSource), source);
    }

    [Fact]
    public async Task GeneticStringHashSource()
    {
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = typeof(HashTableStructure<,>),
            StringAnalyzerConfig = new StringAnalyzerConfig
            {
                BruteForceAnalyzerConfig = null,
                GeneticAnalyzerConfig = new GeneticAnalyzerConfig { RandomSeed = 42 }, // We set the seed for stable runs
                GPerfAnalyzerConfig = null,
                BenchmarkIterations = 0 // We don't benchmark. Contributes to stability
            }
        };

        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["test1", "test2"];
        string source = FastDataGenerator.Generate(keys, config, Generator);

        await VerifyStringHashAsync(HarnessName, nameof(GeneticStringHashSource), source);
    }

    [Fact]
    public async Task GPerfStringHashSource()
    {
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

        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["test1", "test2"];
        string source = FastDataGenerator.Generate(keys, config, Generator);

        await VerifyStringHashAsync(HarnessName, nameof(GPerfStringHashSource), source);
    }
}