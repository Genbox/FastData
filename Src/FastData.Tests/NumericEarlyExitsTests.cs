using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class NumericEarlyExitsTests
{
    [Fact]
    public void GetExits_ConfigDisabled_ReturnsEmpty()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.Disabled = true;

        IEarlyExit[] exits = GetExits([(10, 10), (20, 20)], 10, 2, 10, cfg);
        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_DisabledForStructure_ReturnsEmpty()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableForStructure(typeof(ArrayStructure<,>));

        IEarlyExit[] exits = GetExits([(10, 10), (20, 20)], 10, 2, 10, cfg);
        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_ItemCountAtMinThreshold_ReturnsEmpty()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MinItemCount = 3;

        IEarlyExit[] exits = GetExits([(10, 10), (20, 20)], 10, 2, 3, cfg);
        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_WhenRangeCoversTypeBounds_DoesNotEmitLessOrGreaterThan()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;

        IEarlyExit[] exits = GetExits([(byte.MinValue, byte.MaxValue)], 255, 0, 10, cfg);
        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_SingletonRangeWithGap_EmitsExpectedExitTypes()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueBitMaskEarlyExit));

        IEarlyExit[] exits = GetExits([(10, 10), (20, 30)], 20, 0, 10, cfg);

        Assert.Contains(exits, static x => x is ValueLessThanEarlyExit<int> { Value: 10 });
        Assert.Contains(exits, static x => x is ValueGreaterThanEarlyExit<int> { Value: 30 });
        Assert.Contains(exits, static x => x is ValueInRangeEarlyExit<int> { Min: 10, Max: 20 });
    }

    [Fact]
    public void GetExits_EmitsBitMaskOnlyWhenMaskAndDensityAreValid()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueLessThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueGreaterThanEarlyExit<>));

        IEarlyExit[] valid = GetExits([(10, 10)], 20, 10, 100, cfg);
        Assert.Contains(valid, static x => x is ValueBitMaskEarlyExit { Mask: 10 });

        IEarlyExit[] zeroMask = GetExits([(10, 10)], 20, 0, 100, cfg);
        Assert.DoesNotContain(zeroMask, static x => x is ValueBitMaskEarlyExit);

        IEarlyExit[] allOnesMask = GetExits([(10, 10)], 20, ulong.MaxValue, 100, cfg);
        Assert.DoesNotContain(allOnesMask, static x => x is ValueBitMaskEarlyExit);

        IEarlyExit[] invalidDensity = GetExits([(10, 10)], 50, 10, 100, cfg);
        Assert.DoesNotContain(invalidDensity, static x => x is ValueBitMaskEarlyExit);
    }

    [Fact]
    public void GetExits_WithMaxCandidates_SelectsLargestKeyspacesInOrder()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueBitMaskEarlyExit));
        cfg.DisableEarlyExit(typeof(ValueLessThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueGreaterThanEarlyExit<>));
        cfg.MaxCandidates = 3;

        IEarlyExit[] exits = GetExits([(1, 1), (3, 3), (8, 8), (20, 20), (40, 40)], 39, 0, 20, cfg);

        Assert.Equal(3, exits.Length);
        Assert.All(exits, static x => Assert.IsType<ValueInRangeEarlyExit<int>>(x));

        ulong firstSize = exits[0].KeyspaceSize;
        ulong secondSize = exits[1].KeyspaceSize;
        ulong thirdSize = exits[2].KeyspaceSize;

        Assert.True(firstSize >= secondSize);
        Assert.True(secondSize >= thirdSize);
    }

    private static IEarlyExit[] GetExits<T>(List<(T Start, T End)> ranges, ulong range, ulong bitMask, uint itemCount, EarlyExitConfig cfg)
    {
        DataRanges<T> dataRanges = new DataRanges<T>(ranges.Count);

        foreach ((T Start, T End) r in ranges)
            dataRanges.Add(r.Start, r.End);

        return NumericEarlyExits<T>.GetExits(typeof(ArrayStructure<,>), dataRanges, range, bitMask, itemCount, cfg);
    }
}