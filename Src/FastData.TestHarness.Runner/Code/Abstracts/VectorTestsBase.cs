using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.TestHarness.Runner.Code.Theory;
using static Genbox.FastData.TestHarness.Runner.Code.VerifyHelper;

namespace Genbox.FastData.TestHarness.Runner.Code.Abstracts;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public abstract class VectorTestsBase
{
    private protected abstract TestBase Harness { get; }

    [Theory]
    [InlineData(StructureType.Pgm)]
    [InlineData(StructureType.BinarySearchInterpolation)]
    public async Task IntegerPredictionFindsNumericKeys(StructureType structure)
    {
        long[] wide = Enumerable.Range(0, 512).Select(i => long.MinValue + ((i / 16) * (long.MaxValue / 32)) + ((i % 16) * 2)).ToArray();
        double[] floating = Enumerable.Range(-256, 513).Select(i => i == 0 ? 0 : Math.CopySign(Math.Pow(1.1, Math.Abs(i)), i)).ToArray();
        await CheckAsync(wide, [wide[100] + 1, wide[400] + 1, long.MaxValue]);
        await CheckAsync(floating, [-0.5, 0.5, 1.05], [-0.0]);
        await CheckAsync(new[] { double.MinValue, -double.Epsilon, 0, double.Epsilon, double.MaxValue }, [-1.0, 1.0]);
        await CheckAsync(new[] { long.MinValue, -1, 0, 1, long.MaxValue - 1 }, [-2, 2]);
        await CheckAsync(new ulong[] { 0, 1, 1UL << 63, ulong.MaxValue - 1 }, [2UL, (1UL << 63) + 1]);
        await CheckAsync(Enumerable.Range(0, 256).Select(i => (short)i).ToArray(), new short[] { -1, 256 });
        await CheckAsync(new[] { float.MinValue, -100, -float.Epsilon, 0, float.Epsilon, 100, float.MaxValue }, [-0.5f, 1], [-0.0f]);
        if (structure == StructureType.BinarySearchInterpolation)
            await CheckAsync(new[] { 'a', 'c', 'z' }, ['b', 'y']);

        async Task CheckAsync<TKey>(TKey[] keys, TKey[] misses, TKey[]? aliases = null)
        {
            NumericDataConfig config = new NumericDataConfig { StructureTypeOverride = structure };
            config.EarlyExitConfig.Disabled = true;
            config.StructureSettings.SetSetting(KnownSettings.PgmEpsilon, 4);
            config.StructureSettings.SetSetting(KnownSettings.PgmEpsilonRecursive, 1);
            int[] values = Enumerable.Range(0, keys.Length).ToArray();
            string source = FastDataGenerator.GenerateKeyed(keys, values, config, Harness.Generator).Source;
            string id = $"IntegerPrediction_{structure}_{typeof(TKey).Name}_{keys.Length}";
            Assert.Equal(1, await Harness.RunContainsAsync(source, id + "_contains", aliases == null ? keys : keys.Concat(aliases).ToArray(), misses, TestContext.Current.CancellationToken));
            Assert.Equal(1, await Harness.RunTryLookupAsync(source, id + "_lookup", keys, values, misses, TestContext.Current.CancellationToken));
        }
    }

    [Theory]
    [ClassData(typeof(ValueVectors))]
    public Task ValueVectors(object vector) => (Task)ValueVectorsCoreAsync((dynamic)vector);

    private async Task ValueVectorsCoreAsync<TKey>(TestVector<TKey> vector)
    {
        string source;

        if (vector.Keys is string[] strKeys)
        {
            StringDataConfig config = new StringDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.Generate(strKeys, config, Harness.Generator).Source;
        }
        else
        {
            NumericDataConfig config = new NumericDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.Generate(vector.Keys, config, Harness.Generator).Source;
        }

        Assert.NotEmpty(source);

        string id = $"{nameof(ValueVectors)}_{vector.Identifier}";
        await VerifyVectorAsync(Harness.Name, id, source);
        TKey[] notPresent = vector.StructureType == StructureType.BloomFilter ? [] : vector.NotPresent;
        Assert.Equal(1, await Harness.RunContainsAsync(source, id, vector.Keys, notPresent, TestContext.Current.CancellationToken));

        if (StructureCapabilityHelper.Supports(vector.StructureType, StructureCapability.Enumeration))
            Assert.Equal(1, await Harness.RunKeysEnumerationAsync(source, id + "_keys", TestContext.Current.CancellationToken));

        if (StructureCapabilityHelper.Supports(vector.StructureType, StructureCapability.DirectAccess))
            Assert.Equal(1, await Harness.RunDirectAccessAsync(source, id + "_direct", TestContext.Current.CancellationToken));
    }

    [Theory]
    [ClassData(typeof(KeyValueVectors))]
    public Task KeyValueVectors(object vector) => (Task)KeyValueVectorsCoreAsync((dynamic)vector);

    private async Task KeyValueVectorsCoreAsync<TKey, TValue>(TestVector<TKey, TValue> vector) where TValue : notnull
    {
        string source;

        if (vector.Keys is string[] strKeys)
        {
            StringDataConfig config = new StringDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.GenerateKeyed(strKeys, vector.Values, config, Harness.Generator).Source;
        }
        else
        {
            NumericDataConfig config = new NumericDataConfig();
            config.StructureTypeOverride = vector.StructureType;
            config.EarlyExitConfig.Disabled = true;
            source = FastDataGenerator.GenerateKeyed(vector.Keys, vector.Values, config, Harness.Generator).Source;
        }

        Assert.NotEmpty(source);

        string id = $"{nameof(KeyValueVectors)}_{vector.Identifier}";
        await VerifyFeatureAsync(Harness.Name, id, source);
        TKey[] notPresent = vector.StructureType == StructureType.BloomFilter ? [] : vector.NotPresent;
        Assert.Equal(1, await Harness.RunTryLookupAsync(source, id, vector.Keys, vector.Values, notPresent, TestContext.Current.CancellationToken));

        if (StructureCapabilityHelper.Supports(vector.StructureType, StructureCapability.Enumeration))
        {
            Assert.Equal(1, await Harness.RunKeysEnumerationAsync(source, id + "_keys", TestContext.Current.CancellationToken));
            Assert.Equal(1, await Harness.RunValuesEnumerationAsync(source, id + "_values", TestContext.Current.CancellationToken));
        }

        if (StructureCapabilityHelper.Supports(vector.StructureType, StructureCapability.DirectAccess))
            Assert.Equal(1, await Harness.RunDirectAccessAsync(source, id + "_direct", TestContext.Current.CancellationToken));
    }
}