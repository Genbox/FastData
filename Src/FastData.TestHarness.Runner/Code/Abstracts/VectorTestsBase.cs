using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
using static Genbox.FastData.InternalShared.TestGenerator;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public abstract class VectorTestsBase
{
    protected abstract TestBase Harness { get; }

    [Theory]
    [ClassData(typeof(ValueVectors))]
    public async Task ValueVectors<TKey>(TestVector<TKey> vector)
    {
        string source = Generate(Harness.Generator, vector);
        Assert.NotEmpty(source);

        string id = $"{nameof(ValueVectors)}_{vector.Identifier}";
        await VerifyVectorAsync(Harness.Name, id, source);
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, vector.Keys, vector.NotPresent, TestContext.Current.CancellationToken));
    }

    [Theory]
    [ClassData(typeof(KeyValueVectors))]
    public async Task KeyValueVectors<TKey, TValue>(TestVector<TKey, TValue> vector) where TValue : notnull
    {
        string source = Generate(Harness.Generator, vector);
        Assert.NotEmpty(source);

        string id = $"{nameof(KeyValueVectors)}_{vector.Identifier}";
        await VerifyFeatureAsync(Harness.Name, id, source);
        Assert.Equal(1, await Harness.RunTryLookupAsync(source, id, vector.Keys, vector.Values, vector.NotPresent, TestContext.Current.CancellationToken));
    }
}