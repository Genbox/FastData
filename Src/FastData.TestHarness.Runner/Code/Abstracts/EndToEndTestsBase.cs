using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared.Harness;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class EndToEndTestsBase
{
    private protected abstract TestBase Harness { get; }

    [Fact]
    public async Task IgnoreCaseBinarySearchOrdersAsciiLettersAndPunctuation()
    {
        string[] keys = ["A", "[", "_", "Z", "`", "a?"];
        StringDataConfig config = new StringDataConfig { StructureTypeOverride = StructureType.BinarySearch, IgnoreCase = true };
        config.EarlyExitConfig.Disabled = true;
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;
        Assert.Equal(1, await Harness.RunContainsAsync(source, nameof(IgnoreCaseBinarySearchOrdersAsciiLettersAndPunctuation),
            ["a", "[", "_", "z", "`", "A?"], ["b", "!"], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SupplementaryAndUpperBmpStringKeysMatch()
    {
        string[] keys = ["z", "\U0001F600", "\uE000", "a", "prefix\U0001F600", "prefix\uE000", "m"];
        string[] missing = ["\uE001", "\U0001F601", "prefix\uE001", "absent"];
        int[] values = [11, 22, 33, 44, 55, 66, 77];
        StringDataConfig config = new StringDataConfig { StructureTypeOverride = StructureType.BinarySearch, StringAnalyzerConfig = null };
        config.EarlyExitConfig.Disabled = true;
        StringGenerationResult generated = FastDataGenerator.GenerateKeyed(keys, values, config, Harness.Generator);
        Assert.Equal(1, await Harness.RunTryLookupAsync(generated.Source, nameof(SupplementaryAndUpperBmpStringKeysMatch), keys, values, missing, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenerateIntArrayEndToEndAsync()
    {
        int[] keys = [1, 4, 7, 10, 12];
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = StructureType.Array;

        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;
        const string id = nameof(GenerateIntArrayEndToEndAsync);
        await VerifyEndToEndAsync(Harness.Name, id, source);

        int[] notPresent = [2, 11];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, keys, notPresent, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenerateFloatArrayEndToEndAsync()
    {
        float[] keys = [1.25f, 3.5f, 6.75f, 9.0f];
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = StructureType.Array;

        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;
        const string id = nameof(GenerateFloatArrayEndToEndAsync);
        await VerifyEndToEndAsync(Harness.Name, id, source);

        float[] notPresent = [2f, 8f];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, keys, notPresent, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenerateStringArrayEndToEndAsync()
    {
        string[] keys = ["alpha", "bravo", "charlie", "delta"];
        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = StructureType.Array;

        string source = FastDataGenerator.Generate(keys, config, Harness.Generator).Source;
        const string id = nameof(GenerateStringArrayEndToEndAsync);
        await VerifyEndToEndAsync(Harness.Name, id, source);

        string[] notPresent = ["echo", "foxtrot"];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, keys, notPresent, TestContext.Current.CancellationToken));
    }
}