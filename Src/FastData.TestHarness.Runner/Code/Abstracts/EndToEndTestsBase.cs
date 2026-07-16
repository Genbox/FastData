using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared.Harness;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class EndToEndTestsBase
{
    protected abstract TestBase Harness { get; }

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