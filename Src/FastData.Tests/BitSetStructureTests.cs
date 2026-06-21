using System;
using Genbox.FastData.Config;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class BitSetStructureTests
{
    [Fact]
    public void DenseValues_ContiguousZeroBased_UsesNoOccupancy()
    {
        ReadOnlyMemory<int> keys = (int[])[0, 1, 2];
        ReadOnlyMemory<string> values = (string[])["zero", "one", "two"];

        BitSetStructure<int, string> structure = new BitSetStructure<int, string>(KeyAnalyzer.GetNumericProperties(keys));
        BitSetContext<string>? context = structure.Create(keys, values);

        Assert.NotNull(context);
        Assert.False(context.HasOccupancy);
        Assert.Empty(context.BitSet);
        Assert.Equal(new[] { "zero", "one", "two" }, context.Values.ToArray());
    }

    [Fact]
    public void DenseValues_OffsetContiguous_UsesNoOccupancy()
    {
        ReadOnlyMemory<int> keys = (int[])[10, 11, 12];
        ReadOnlyMemory<int> values = (int[])[100, 110, 120];

        BitSetStructure<int, int> structure = new BitSetStructure<int, int>(KeyAnalyzer.GetNumericProperties(keys));
        BitSetContext<int>? context = structure.Create(keys, values);

        Assert.NotNull(context);
        Assert.False(context.HasOccupancy);
        Assert.Empty(context.BitSet);
        Assert.Equal(new[] { 100, 110, 120 }, context.Values.ToArray());
    }

    [Fact]
    public void DenseValues_SparseDense_UsesOccupancyAndLeavesMissingValuesDefault()
    {
        ReadOnlyMemory<int> keys = (int[])[0, 1, 3];
        ReadOnlyMemory<int> values = (int[])[10, 20, 40];

        BitSetStructure<int, int> structure = new BitSetStructure<int, int>(KeyAnalyzer.GetNumericProperties(keys));
        BitSetContext<int>? context = structure.Create(keys, values);

        Assert.NotNull(context);
        Assert.True(context.HasOccupancy);
        Assert.Equal(0b1011UL, context.BitSet[0]);
        Assert.Equal(new[] { 10, 20, 0, 40 }, context.Values.ToArray());
    }

    [Fact]
    public void DenseValues_DensityLimitFallback_UsesConditionalWhenRangeFactorRejected()
    {
        ReadOnlyMemory<int> keys = (int[])[0, 1, 10];

        Type selected = NumericStructures<int>.GetBest(keys, true, KeyAnalyzer.GetNumericProperties(keys).Density,
            false, 2, 10, StructureCapability.Membership | StructureCapability.KeyValueLookup, 2f, StructureConfig.Default,
            static _ => throw new InvalidOperationException("Hash data should not be needed."));

        Assert.Equal(typeof(ConditionalStructure<,>), selected);
    }

    [Fact]
    public void HandlesUInt64ValuesAboveInt64Max()
    {
        ReadOnlyMemory<ulong> keys = (ulong[])[9_223_372_036_854_775_804UL, 9_223_372_036_854_775_812UL];
        ReadOnlyMemory<int> values = (int[])[10, 20];

        BitSetStructure<ulong, int> structure = new BitSetStructure<ulong, int>(KeyAnalyzer.GetNumericProperties(keys));
        BitSetContext<int>? context = structure.Create(keys, values);

        Assert.NotNull(context);
        Assert.Equal(0b1_0000_0001UL, context.BitSet[0]);
        Assert.Equal(new[] { 10, 0, 0, 0, 0, 0, 0, 0, 20 }, context.Values.ToArray());
    }
}