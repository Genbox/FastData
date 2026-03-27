using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Config;
using Genbox.FastData.Generators;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
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
        string source;

        if (vector.Keys is string[] strKeys)
        {
            StringDataConfig config = new StringDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.Generate(strKeys, config, Harness.Generator);
        }
        else
        {
            NumericDataConfig config = new NumericDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.Generate(vector.Keys, config, Harness.Generator);
        }

        Assert.NotEmpty(source);

        string id = $"{nameof(ValueVectors)}_{vector.Identifier}";
        await VerifyVectorAsync(Harness.Name, id, source);
        TKey[] notPresent = vector.StructureType == typeof(BloomFilterStructure<,>) ? Array.Empty<TKey>() : vector.NotPresent;
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, vector.Keys, notPresent, TestContext.Current.CancellationToken));
    }

    [Theory]
    [ClassData(typeof(KeyValueVectors))]
    public async Task KeyValueVectors<TKey, TValue>(TestVector<TKey, TValue> vector) where TValue : notnull
    {
        string source;

        if (vector.Keys is string[] strKeys)
        {
            StringDataConfig config = new StringDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.GenerateKeyed(strKeys, vector.Values, config, Harness.Generator);
        }
        else
        {
            NumericDataConfig config = new NumericDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.GenerateKeyed(vector.Keys, vector.Values, config, Harness.Generator);
        }

        Assert.NotEmpty(source);

        string id = $"{nameof(KeyValueVectors)}_{vector.Identifier}";
        await VerifyFeatureAsync(Harness.Name, id, source);
        TKey[] notPresent = vector.StructureType == typeof(BloomFilterStructure<,>) ? Array.Empty<TKey>() : vector.NotPresent;
        Assert.Equal(1, await Harness.RunTryLookupAsync(source, id, vector.Keys, vector.Values, notPresent, TestContext.Current.CancellationToken));
    }
}