using Genbox.FastData.Config;
using Genbox.FastData.Internal.Structures;
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
        config.StructureTypeOverride = typeof(ArrayStructure<,>);

        string output = FastDataGenerator.Generate(keys, config, Harness.Generator);
        string id = nameof(GenerateIntArrayEndToEndAsync);
        await VerifyEndToEndAsync(Harness.Name, id, output);

        int[] notPresent = [2, 11];
        Assert.Equal(1, await Harness.RunContainsAsync(output, id, keys, notPresent, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenerateFloatArrayEndToEndAsync()
    {
        float[] keys = [1.25f, 3.5f, 6.75f, 9.0f];
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = typeof(ArrayStructure<,>);

        string output = FastDataGenerator.Generate(keys, config, Harness.Generator);
        string id = nameof(GenerateFloatArrayEndToEndAsync);
        await VerifyEndToEndAsync(Harness.Name, id, output);

        float[] notPresent = [2f, 8f];
        Assert.Equal(1, await Harness.RunContainsAsync(output, id, keys, notPresent, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenerateStringArrayEndToEndAsync()
    {
        string[] keys = ["alpha", "bravo", "charlie", "delta"];
        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = typeof(ArrayStructure<,>);

        string output = FastDataGenerator.Generate(keys, config, Harness.Generator);
        string id = nameof(GenerateStringArrayEndToEndAsync);
        await VerifyEndToEndAsync(Harness.Name, id, output);

        string[] notPresent = ["echo", "foxtrot"];
        Assert.Equal(1, await Harness.RunContainsAsync(output, id, keys, notPresent, TestContext.Current.CancellationToken));
    }
}
