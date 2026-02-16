using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestHarness;
using Genbox.FastData.TestHarness.Runner.Code;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.TestHarness.Runner;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public class VectorTests
{
    [Theory]
    [ClassData(typeof(KeyValueTestVectors))]
    public async Task KeyValueVectors<TKey, TValue>(ITestHarness harness, TestVector<TKey, TValue> vector) where TValue : notnull
    {
        TestHarnessRunnerHelper.SkipIfEmptyImplementation(harness, vector.Type);
        GeneratorSpec spec = Generate(harness.CreateGenerator, vector);
        Assert.NotEmpty(spec.Source);

        string snapshotId = $"{nameof(KeyValueVectors)}_{spec.Identifier}";

        await TestHarnessRunnerHelper.VerifyFeatureAsync(harness, snapshotId, spec.Source);
        int exitCode = TestHarnessRunnerHelper.RunTryLookupProgram(harness, spec, vector, snapshotId);
        TestHarnessRunnerHelper.AssertSuccessExitCode(exitCode);
    }

    [Theory]
    [ClassData(typeof(ValueTestVectors))]
    public async Task ValueVectors<T>(ITestHarness harness, TestVector<T> vector)
    {
        TestHarnessRunnerHelper.SkipIfEmptyImplementation(harness, vector.Type);
        GeneratorSpec spec = Generate(harness.CreateGenerator, vector);
        Assert.NotEmpty(spec.Source);

        string snapshotId = $"{nameof(ValueVectors)}_{spec.Identifier}";

        await TestHarnessRunnerHelper.VerifyVectorAsync(harness, snapshotId, spec.Source);
        int exitCode = TestHarnessRunnerHelper.RunContainsProgram(harness, spec, vector.Keys, vector.NotPresent, snapshotId);
        TestHarnessRunnerHelper.AssertSuccessExitCode(exitCode);
    }

    private sealed class ValueTestVectors : HarnessVectorTheoryData
    {
        public ValueTestVectors() => AddVectors(TestVectorHelper.GetValueTestVectors().ToArray());
    }

    private sealed class KeyValueTestVectors : HarnessVectorTheoryData
    {
        public KeyValueTestVectors() => AddVectors(TestVectorHelper.GetKeyValueTestVectors().ToArray());
    }
}