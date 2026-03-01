using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;
using static Genbox.FastData.TestHarness.Runner.Code.TestHarnessHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public abstract class FeatureTestBase
{
    protected abstract TestBase Harness { get; }

    [Theory]
    [ClassData(typeof(FloatNaNZeroTestVectors))]
    public async Task FloatNaNOrZeroHashSupport<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(Harness.CreateGenerator, vector);

        string snapshotId = $"{nameof(FloatNaNOrZeroHashSupport)}_{spec.Identifier}";

        await VerifyFeatureAsync(Harness, snapshotId, spec.Source);

        AssertSuccessExitCode(RunContains(Harness, spec, vector.Keys, vector.NotPresent, snapshotId));
    }

    [Fact]
    public async Task IgnoreCaseSupport()
    {
        string[] keys = ["Alpha", "bravo", "CHARLIE"];

        FastDataConfig config = new FastDataConfig(StructureType.BinarySearch);
        config.IgnoreCase = true;

        const string className = nameof(IgnoreCaseSupport);
        string source = FastDataGenerator.Generate(keys, config, Harness.CreateGenerator(className));
        GeneratorSpec spec = new GeneratorSpec(className, source);

        await VerifyFeatureAsync(Harness, className, spec.Source);

        string[] lookups = ["alpha", "BRAVO", "charlie"];
        string[] notPresent = ["delta", "echo"];
        AssertSuccessExitCode(RunContains(Harness, spec, lookups, notPresent, className));
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public async Task PrefixSuffixTrimming(bool ignoreCase)
    {
        string[] keys = ["PreAlphaSuf", "PreBravoSuf", "PreCharlieSuf"];

        FastDataConfig config = new FastDataConfig(StructureType.BinarySearch);
        config.EnablePrefixSuffixTrimming = true;
        config.IgnoreCase = ignoreCase;

        string className = $"{nameof(PrefixSuffixTrimming)}_{(ignoreCase ? "IgnoreCase" : "Ordinal")}";
        string source = FastDataGenerator.Generate(keys, config, Harness.CreateGenerator(className));
        GeneratorSpec spec = new GeneratorSpec(className, source);

        await VerifyFeatureAsync(Harness, className, spec.Source);

        string[] lookups = ignoreCase ? ["prealphasuf", "PREBRAVOSUF", "precharliesuf"] : keys;
        string[] notPresent = ["PreDeltaSuf", "preEchoSuf", "Nope"];

        AssertSuccessExitCode(RunContains(Harness, spec, lookups, notPresent, className));
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public async Task TypeReductionEnabled(bool typeReductionEnabled)
    {
        FastDataConfig config = new FastDataConfig(StructureType.HashTable);
        config.TypeReductionEnabled = typeReductionEnabled;

        string className = $"{nameof(TypeReductionEnabled)}_{typeReductionEnabled}";
        byte[] keys = [byte.MinValue, 1, byte.MaxValue];

        string source = FastDataGenerator.Generate(keys, config, Harness.CreateGenerator(className));
        GeneratorSpec spec = new GeneratorSpec(className, source);

        await VerifyFeatureAsync(Harness, className, spec.Source);

        byte[] notPresent = [2, 4];
        AssertSuccessExitCode(RunContains(Harness, spec, keys, notPresent, className));
    }
}