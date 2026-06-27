using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class RoundModuloToPowerOfTwoTests
{
    [Fact]
    public void NumericConfigDefaultsEnableHashTableRounding()
    {
        NumericDataConfig config = new NumericDataConfig { StructureTypeOverride = typeof(HashTableStructure<,>) };

        CapturingGenerator generator = new CapturingGenerator();
        FastDataGenerator.Generate([0, 1, 2, 3, 4, 5, 6], config, generator);
        HashTableContext<int, byte> context = Assert.IsType<HashTableContext<int, byte>>(generator.Context);

        Assert.Equal(8, context.Buckets.Length);
    }

    [Fact]
    public void NumericConfigDefaultsDoNotRoundOutsideThreshold()
    {
        NumericDataConfig config = new NumericDataConfig { StructureTypeOverride = typeof(HashTableStructure<,>) };

        CapturingGenerator generator = new CapturingGenerator();
        FastDataGenerator.Generate([0, 1, 2, 3, 4, 5], config, generator);
        HashTableContext<int, byte> context = Assert.IsType<HashTableContext<int, byte>>(generator.Context);

        Assert.Equal(6, context.Buckets.Length);
    }

    [Fact]
    public void NumericConfigDefaultsEnableBloomFilterRounding()
    {
        NumericDataConfig config = new NumericDataConfig
        {
            AllowApproximation = true,
            StructureTypeOverride = typeof(BloomFilterStructure<,>)
        };
        config.StructureSettings.SetSetting(KnownSettings.RoundModuloToPowerOfTwoThreshold, 0.34f);

        CapturingGenerator generator = new CapturingGenerator();
        FastDataGenerator.Generate([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12], config, generator);
        BloomFilterContext context = Assert.IsType<BloomFilterContext>(generator.Context);

        Assert.Equal(4, context.BitSet.Length);
    }

    [Fact]
    public void NumericConfigDefaultsEnableBucketSizeOptimization()
    {
        NumericDataConfig config = new NumericDataConfig { StructureTypeOverride = typeof(HashTableStructure<,>) };
        config.StructureSettings.SetSetting(KnownSettings.RoundModuloToPowerOfTwo, false);

        CapturingGenerator generator = new CapturingGenerator();
        FastDataGenerator.Generate([0, 6, 12], config, generator);
        HashTableContext<int, byte> context = Assert.IsType<HashTableContext<int, byte>>(generator.Context);

        Assert.Equal(5, context.Buckets.Length);
    }

    [Fact]
    public void HashTableOptimizesBucketSizeWhenEnabled()
    {
        int[] keys = [0, 6, 12];
        HashData hashData = HashData.Create(keys, 1f, true, false, 0, static x => (ulong)x);
        HashTableStructure<int, byte> structure = new HashTableStructure<int, byte>(hashData);
        HashTableContext<int, byte> context = structure.Create(keys, ReadOnlyMemory<byte>.Empty);

        Assert.Equal(5, context.Buckets.Length);
        Assert.True(hashData.HashCodesPerfect);
    }

    [Fact]
    public void HashTableBucketOptimizationPreservesPowerOfTwoRounding()
    {
        int[] keys = [0, 1, 2, 3, 4, 5, 6];
        HashData hashData = HashData.Create(keys, 1f, true, true, 0.15f, static x => (ulong)x);
        HashTableStructure<int, byte> structure = new HashTableStructure<int, byte>(hashData);
        HashTableContext<int, byte> context = structure.Create(keys, ReadOnlyMemory<byte>.Empty);

        Assert.Equal(8, context.Buckets.Length);
    }

    [Theory]
    [InlineData(0.15f, 8)]
    [InlineData(0.10f, 7)]
    public void HashTableUsesRoundedLengthWhenWithinThreshold(float threshold, int expectedLength)
    {
        int[] keys = [0, 1, 2, 3, 4, 5, 6];
        HashData hashData = HashData.Create(keys, 1f, true, threshold, static x => (ulong)x);
        HashTableStructure<int, byte> structure = new HashTableStructure<int, byte>(hashData);
        HashTableContext<int, byte> context = structure.Create(keys, ReadOnlyMemory<byte>.Empty);

        Assert.Equal(expectedLength, context.Buckets.Length);
    }

    [Theory]
    [InlineData(0.15f, 8)]
    [InlineData(0.10f, 7)]
    public void HashTableCompactUsesRoundedLengthWhenWithinThreshold(float threshold, int expectedLength)
    {
        int[] keys = [0, 1, 2, 3, 4, 5, 6];
        HashData hashData = HashData.Create(keys, 1f, true, threshold, static x => (ulong)x);
        HashTableCompactStructure<int, byte> structure = new HashTableCompactStructure<int, byte>(hashData);
        HashTableCompactContext<int, byte> context = structure.Create(keys, ReadOnlyMemory<byte>.Empty);

        Assert.Equal(expectedLength + 1, context.BucketStarts.Length);
    }

    [Theory]
    [InlineData(0.15f, 8)]
    [InlineData(0.10f, 7)]
    public void HashTablePerfectUsesRoundedLengthWhenWithinThreshold(float threshold, int expectedLength)
    {
        int[] keys = [0, 1, 2, 3, 4, 5, 6];
        HashData hashData = HashData.Create(keys, 1f, true, threshold, static x => (ulong)x);
        HashTablePerfectStructure<int, byte> structure = new HashTablePerfectStructure<int, byte>(hashData);
        HashTablePerfectContext<int, byte> context = structure.Create(keys, ReadOnlyMemory<byte>.Empty);

        Assert.Equal(expectedLength, context.Data.Length);
    }

    [Theory]
    [InlineData(0.34f, 4)]
    [InlineData(0.20f, 3)]
    public void BloomFilterUsesRoundedWordLengthWhenWithinThreshold(float threshold, int expectedLength)
    {
        int[] keys = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        HashData hashData = HashData.Create(keys, 1f, true, threshold, static x => (ulong)x);
        BloomFilterStructure<int, byte> structure = new BloomFilterStructure<int, byte>(hashData);
        BloomFilterContext context = structure.Create(keys, ReadOnlyMemory<byte>.Empty);

        Assert.Equal(expectedLength, context.BitSet.Length);
    }

    private sealed class CapturingGenerator : ICodeGenerator
    {
        public IContext? Context { get; private set; }
        public GeneratorEncoding Encoding => GeneratorEncoding.Utf8Bytes;

        public string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context)
        {
            Context = context;
            return string.Empty;
        }
    }
}