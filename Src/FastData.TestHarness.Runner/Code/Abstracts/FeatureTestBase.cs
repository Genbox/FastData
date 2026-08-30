using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class FeatureTestBase
{
    private protected abstract TestBase Harness { get; }

    [Fact]
    public async Task FloatNaNOrZeroHashSupport()
    {
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = StructureType.HashTable;
        config.EarlyExitConfig.Disabled = true;

        float[] floats = [1f, 2f, 3f, 4f, 5f];
        string source = FastDataGenerator.Generate(floats, config, Harness.Generator).Source;
        string id = $"{nameof(FloatNaNOrZeroHashSupport)}_Float";
        await VerifyFeatureAsync(Harness.Name, id, source);
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, floats, [], TestContext.Current.CancellationToken));

        double[] doubles = [1.0, 2.0, 3.0, 4.0, 5.0];
        source = FastDataGenerator.Generate(doubles, config, Harness.Generator).Source;
        id = $"{nameof(FloatNaNOrZeroHashSupport)}_Double";
        await VerifyFeatureAsync(Harness.Name, id, source);
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, doubles, [], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RoundModuloToPowerOfTwoSupport()
    {
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = StructureType.HashTable;
        config.EarlyExitConfig.Disabled = true;

        int[] keys = [0, 1, 2, 3, 4, 5, 6];
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;
        const string id = nameof(RoundModuloToPowerOfTwoSupport);
        await VerifyFeatureAsync(Harness.Name, id, source);

        Assert.Equal(1, await Harness.RunContainsAsync(source, id, keys, [7, 8], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FractionalFloatingPointLiteralsMatch()
    {
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = StructureType.Array;
        config.EarlyExitConfig.Disabled = true;

        float[] floats = [1.25f, 2.5f, 3.75f];
        string source = FastDataGenerator.Generate(floats, config, Harness.Generator).Source;
        Assert.Equal(1, await Harness.RunContainsAsync(source, nameof(FractionalFloatingPointLiteralsMatch) + "_Float", floats, [1.2f, 2.4f], TestContext.Current.CancellationToken));

        double[] doubles = [1.23, 2.5, 3.75];
        source = FastDataGenerator.Generate(doubles, config, Harness.Generator).Source;
        Assert.Equal(1, await Harness.RunContainsAsync(source, nameof(FractionalFloatingPointLiteralsMatch) + "_Double", doubles, [1.2, 2.4], TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public async Task IgnoreCaseSupport(bool ignoreCase)
    {
        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = StructureType.BinarySearch;
        config.EarlyExitConfig.Disabled = true;
        config.IgnoreCase = ignoreCase;

        string[] keys = ["Alpha", "bravo", "CHARLIE"];
        string source = FastDataGenerator.Generate(keys, config, GetIgnoreCaseGenerator(ignoreCase)).Source;

        string id = $"{nameof(IgnoreCaseSupport)}_{(ignoreCase ? "IgnoreCase" : "Ordinal")}";
        await VerifyFeatureAsync(Harness.Name, id, source);

        string[] lookups = ignoreCase ? ["alpha", "BRAVO", "charlie"] : keys;
        string[] notPresent = ["delta", "echo"];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, lookups, notPresent, TestContext.Current.CancellationToken));
    }

    protected virtual ICodeGenerator GetIgnoreCaseGenerator(bool ignoreCase) => Harness.Generator;

    [Fact]
    public async Task SpecialCharacterStringLiteralsCompileAndMatch()
    {
        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = StructureType.Array;
        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["quote\"key", "slash\\key", "line\nkey", "tab\tkey"];
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;

        Assert.Equal(1, await Harness.RunContainsAsync(source, nameof(SpecialCharacterStringLiteralsCompileAndMatch), keys, ["missing"], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HybleStringHashFilterMembershipSupport()
    {
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = StructureType.Hyble,
            StringAnalyzerConfig = null
        };
        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["alpha-0001", "bravo-0002", "charlie-003", "delta-0004", "echo-00005", "foxtrot-006"];
        string[] notPresent = ["alpha-0002", "bravo-0003", "charlie-004", "delta-0005", "echo-00006", "foxtrot-007"];
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;

        Assert.Equal(1, await Harness.RunContainsAsync(source, nameof(HybleStringHashFilterMembershipSupport), keys, notPresent, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HybleStringHashFilterIgnoreCaseKeyValueSupport()
    {
        StringDataConfig config = new StringDataConfig
        {
            StructureTypeOverride = StructureType.Hyble,
            IgnoreCase = true,
            StringAnalyzerConfig = null
        };
        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["ALPHA-0001", "Bravo-0002", "CHARLIE-003", "Delta-0004", "ECHO-00005", "Foxtrot-006"];
        string[] lookups = ["alpha-0001", "BRAVO-0002", "charlie-003", "DELTA-0004", "echo-00005", "FOXTROT-006"];
        string[] notPresent = ["alpha-0002", "bravo-0003", "charlie-004", "delta-0005", "echo-00006", "foxtrot-007"];
        int[] values = [11, 22, 33, 44, 55, 66];
        string source = FastDataGenerator.GenerateKeyed(keys, values, config, GetIgnoreCaseGenerator(true)).Source;

        Assert.Equal(1, await Harness.RunTryLookupAsync(source, nameof(HybleStringHashFilterIgnoreCaseKeyValueSupport), lookups, values, notPresent, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public async Task TypeReductionSupported(bool enabled)
    {
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = StructureType.HashTable;
        config.EarlyExitConfig.Disabled = true;
        config.TypeReductionEnabled = enabled;

        byte[] keys = [byte.MinValue, 1, byte.MaxValue];
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;

        string id = $"{nameof(TypeReductionSupported)}_{enabled}";
        await VerifyFeatureAsync(Harness.Name, id, source);

        byte[] notPresent = [2, 4];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, keys, notPresent, TestContext.Current.CancellationToken));
    }
}