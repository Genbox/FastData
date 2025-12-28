using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestHarness;
using Genbox.FastData.TestHarness.Runner.Code;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.TestHarness.Runner;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public class FeatureTests
{
    [Theory]
    [ClassData(typeof(FloatNaNZeroTestVectors))]
    public async Task FloatNaNOrZeroHashSupport<T>(ITestHarness harness, TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(harness.CreateGenerator, vector);

        string snapshotId = $"{nameof(FloatNaNOrZeroHashSupport)}_{spec.Identifier}";

        await TestHarnessRunnerHelper.VerifyFeatureAsync(harness, snapshotId, spec.Source);

        int exitCode = TestHarnessRunnerHelper.RunContainsProgram(harness, spec, vector.Keys, vector.NotPresent, snapshotId);
        TestHarnessRunnerHelper.AssertSuccessExitCode(exitCode);
    }

    [Theory]
    [ClassData(typeof(HarnessTheoryData))]
    public async Task IgnoreCaseSupport(ITestHarness harness)
    {
        string[] keys = ["Alpha", "bravo", "CHARLIE"];

        FastDataConfig config = new FastDataConfig(StructureType.BinarySearch) { IgnoreCase = true };

        const string className = nameof(IgnoreCaseSupport);
        string source = FastDataGenerator.Generate(keys, config, harness.CreateGenerator(className));
        GeneratorSpec spec = new GeneratorSpec(className, source);

        await TestHarnessRunnerHelper.VerifyFeatureAsync(harness, className, spec.Source);

        string[] lookups = ["alpha", "BRAVO", "charlie"];
        string[] notPresent = ["delta", "echo"];
        int exitCode = TestHarnessRunnerHelper.RunContainsProgram(harness, spec, lookups, notPresent, className);
        TestHarnessRunnerHelper.AssertSuccessExitCode(exitCode);
    }

    [Theory]
    [ClassData(typeof(HarnessBoolTheoryData))]
    public async Task PrefixSuffixTrimming(ITestHarness harness, bool ignoreCase)
    {
        string[] keys = ["PreAlphaSuf", "PreBravoSuf", "PreCharlieSuf"];
        FastDataConfig config = new FastDataConfig(StructureType.BinarySearch)
        {
            EnablePrefixSuffixTrimming = true,
            IgnoreCase = ignoreCase
        };

        string caseLabel = ignoreCase ? "IgnoreCase" : "Ordinal";
        string className = $"{nameof(PrefixSuffixTrimming)}_{caseLabel}";
        string source = FastDataGenerator.Generate(keys, config, harness.CreateGenerator(className));
        GeneratorSpec spec = new GeneratorSpec(className, source);

        await TestHarnessRunnerHelper.VerifyFeatureAsync(harness, className, spec.Source);

        string[] lookups = ignoreCase ? ["prealphasuf", "PREBRAVOSUF", "precharliesuf"] : keys;
        string[] notPresent = ["PreDeltaSuf", "preEchoSuf", "Nope"];

        int exitCode = TestHarnessRunnerHelper.RunContainsProgram(harness, spec, lookups, notPresent, className);
        TestHarnessRunnerHelper.AssertSuccessExitCode(exitCode);
    }
}