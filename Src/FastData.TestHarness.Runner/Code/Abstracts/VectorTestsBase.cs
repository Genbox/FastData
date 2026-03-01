using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;
using static Genbox.FastData.TestHarness.Runner.Code.TestHarnessHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public abstract class VectorTestsBase
{
    protected abstract TestBase Harness { get; }

    [Theory]
    [ClassData(typeof(KeyValueVectors))]
    public async Task KeyValueVectors<TKey, TValue>(TestVector<TKey, TValue> vector) where TValue : notnull
    {
        GeneratorSpec spec = Generate(Harness.CreateGenerator, vector);
        Assert.NotEmpty(spec.Source);

        string snapshotId = $"{nameof(KeyValueVectors)}_{spec.Identifier}";

        await VerifyFeatureAsync(Harness, snapshotId, spec.Source);
        AssertSuccessExitCode(RunTryLookup(Harness, spec, vector, snapshotId));
    }

    [Theory]
    [ClassData(typeof(ValueVectors))]
    public async Task ValueVectors<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(Harness.CreateGenerator, vector);
        Assert.NotEmpty(spec.Source);

        string snapshotId = $"{nameof(ValueVectors)}_{spec.Identifier}";

        await VerifyVectorAsync(Harness, snapshotId, spec.Source);
        AssertSuccessExitCode(RunContains(Harness, spec, vector.Keys, vector.NotPresent, snapshotId));
    }
}