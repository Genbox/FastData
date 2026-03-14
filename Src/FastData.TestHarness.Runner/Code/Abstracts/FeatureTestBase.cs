using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.Harness;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

public abstract class FeatureTestBase
{
    protected abstract TestBase Harness { get; }

    [Fact]
    public async Task FloatNaNOrZeroHashSupport()
    {
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = typeof(HashTableStructure<,>);

        float[] floats = [1f, 2f, 3f, 4f, 5f];
        string source = FastDataGenerator.Generate(floats, config, Harness.Generator);
        string id = $"{nameof(FloatNaNOrZeroHashSupport)}_Float";
        await VerifyFeatureAsync(Harness.Name, id, source);
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, floats, [], TestContext.Current.CancellationToken));

        float[] doubles = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
        source = FastDataGenerator.Generate(doubles, config, Harness.Generator);
        id = $"{nameof(FloatNaNOrZeroHashSupport)}_Double";
        await VerifyFeatureAsync(Harness.Name, id, source);
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, doubles, [], TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public async Task IgnoreCaseSupport(bool ignoreCase)
    {
        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = typeof(BinarySearchStructure<,>);
        config.IgnoreCase = ignoreCase;

        string[] keys = ["Alpha", "bravo", "CHARLIE"];
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator);

        string id = $"{nameof(IgnoreCaseSupport)}_{(ignoreCase ? "IgnoreCase" : "Ordinal")}";
        await VerifyFeatureAsync(Harness.Name, id, source);

        string[] lookups = ignoreCase ? ["alpha", "BRAVO", "charlie"] : keys;
        string[] notPresent = ["delta", "echo"];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, lookups, notPresent, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public async Task PrefixSuffixTrimmingSupported(bool enabled)
    {
        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = typeof(BinarySearchStructure<,>);
        config.EnablePrefixSuffixTrimming = enabled;

        string[] keys = ["PreAlphaSuf", "PreBravoSuf", "PreCharlieSuf"];
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator);

        string id = $"{nameof(PrefixSuffixTrimmingSupported)}_{(enabled ? "TrimEnabled" : "TrimDisabled")}";
        await VerifyFeatureAsync(Harness.Name, id, source);

        string[] notPresent = ["PreDeltaSuf", "preEchoSuf", "Nope"];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, keys, notPresent, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public async Task TypeReductionSupported(bool enabled)
    {
        NumericDataConfig config = new NumericDataConfig();
        config.StructureTypeOverride = typeof(HashTableStructure<,>);
        config.TypeReductionEnabled = enabled;

        byte[] keys = [byte.MinValue, 1, byte.MaxValue];
        string source = FastDataGenerator.Generate(keys, config, Harness.Generator);

        string id = $"{nameof(TypeReductionSupported)}_{enabled}";
        await VerifyFeatureAsync(Harness.Name, id, source);

        byte[] notPresent = [2, 4];
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, keys, notPresent, TestContext.Current.CancellationToken));
    }
}