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
        cfg.MinRejectionRatio = 0f;

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
        cfg.MinRejectionRatio = 0f;

        IEarlyExit[] valid = GetExits([(10, 10)], 20, 10, 100, cfg);
        Assert.Contains(valid, static x => x is ValueBitMaskEarlyExit { Mask: 10 });

        IEarlyExit[] zeroMask = GetExits([(10, 10)], 20, 0, 100, cfg);
        Assert.DoesNotContain(zeroMask, static x => x is ValueBitMaskEarlyExit);

        IEarlyExit[] allOnesMask = GetExits([(10, 10)], 20, ulong.MaxValue, 100, cfg);
        Assert.DoesNotContain(allOnesMask, static x => x is ValueBitMaskEarlyExit);

        IEarlyExit[] invalidDensity = GetExits([(10, 10)], 50, 1, 100, cfg);
        Assert.DoesNotContain(invalidDensity, static x => x is ValueBitMaskEarlyExit);
    }

    [Fact]
    public void GetExits_WithMaxCandidates_SelectsLargestKeyspacesInOrder()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueBitMaskEarlyExit));
        cfg.DisableEarlyExit(typeof(ValueLessThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueGreaterThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueBitSetEarlyExit<>));
        cfg.MaxCandidates = 3;
        cfg.MinRejectionRatio = 0f;

        IEarlyExit[] exits = GetExits([(1, 1), (3, 3), (8, 8), (20, 20), (40, 40)], 39, 0, 20, cfg);

        Assert.Equal(3, exits.Length);
        Assert.All(exits, static x => Assert.IsType<ValueInRangeEarlyExit<int>>(x));

        ulong firstSize = exits[0].KeyspaceSize;
        ulong secondSize = exits[1].KeyspaceSize;
        ulong thirdSize = exits[2].KeyspaceSize;

        Assert.True(firstSize >= secondSize);
        Assert.True(secondSize >= thirdSize);
    }

    [Fact]
    public void GetExits_RejectionBelowThreshold_DiscardsRangeExit()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueBitMaskEarlyExit));
        cfg.DisableEarlyExit(typeof(ValueLessThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueGreaterThanEarlyExit<>));
        cfg.MinRejectionRatio = 0.5f;

        IEarlyExit[] exits = GetExits([(10, 10), (12, 12)], 2, 0, 10, cfg);

        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_ObservedRangeBaseline_KeepsLessThanExit()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueGreaterThanEarlyExit<>));

        // Observed range is tiny (1000..1003), so the less-than exit covers a large share of the observed span.
        IEarlyExit[] exits = GetExits([(1000u, 1003u)], 3, 0, 4, cfg);
        Assert.Contains(exits, static x => x is ValueLessThanEarlyExit<uint> { Value: 1000u });

        // Observed-range ratio is used to keep the exit.
        ValueLessThanEarlyExit<uint> exit = new ValueLessThanEarlyExit<uint>(1000u);
        Assert.True(exit.KeyspaceSize / ((1003u - 1000u) + 1d) >= 0.5d);
    }

    [Fact]
    public void GetExits_CharKeys_EmitsLowByteBitmapExit()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueBitMaskEarlyExit));
        cfg.DisableEarlyExit(typeof(ValueLessThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueGreaterThanEarlyExit<>));
        cfg.MinRejectionRatio = 0f;

        char[] keys = ['a', 'e', 'i', 'm', 'q', 'u', 'y', '}', '\u0081'];
        DataRanges<char> dataRanges = new DataRanges<char>(1);
        dataRanges.Add('a', '\u0081');

        IEarlyExit[] exits = NumericEarlyExits<char>.GetExits(typeof(ConditionalStructure<,>), keys, dataRanges, '\u0081' - 'a', 0, (uint)keys.Length, cfg);

        ValueLowByteBitmapEarlyExit exit = Assert.IsType<ValueLowByteBitmapEarlyExit>(Assert.Single(exits));
        Assert.Equal(247UL, exit.KeyspaceSize);
    }

    [Fact]
    public void GetExits_IntKeys_EmitsLowByteBitmapExit()
    {
        EarlyExitConfig cfg = LowByteOnlyConfig();
        int[] keys = [0x100, 0x200, 0x300, 0x400, 0x500, 0x600, 0x700, 0x800, 0x900];
        DataRanges<int> dataRanges = new DataRanges<int>(1);
        dataRanges.Add(0x100, 0x900);

        IEarlyExit[] exits = NumericEarlyExits<int>.GetExits(typeof(ConditionalStructure<,>), keys, dataRanges, 0x800, 0, (uint)keys.Length, cfg);

        ValueLowByteBitmapEarlyExit exit = Assert.IsType<ValueLowByteBitmapEarlyExit>(Assert.Single(exits));
        Assert.Equal(255UL, exit.KeyspaceSize);
        Assert.Equal(1f / 256f, exit.AcceptedDensity);
    }

    [Fact]
    public void GetExits_LowByteBitmapDisabled_DoesNotEmitLowByteBitmapExit()
    {
        EarlyExitConfig cfg = LowByteOnlyConfig();
        cfg.DisableEarlyExit(typeof(ValueLowByteBitmapEarlyExit));
        int[] keys = [0x100, 0x200, 0x300, 0x400, 0x500, 0x600, 0x700, 0x800, 0x900];
        DataRanges<int> dataRanges = new DataRanges<int>(1);
        dataRanges.Add(0x100, 0x900);

        IEarlyExit[] exits = NumericEarlyExits<int>.GetExits(typeof(ConditionalStructure<,>), keys, dataRanges, 0x800, 0, (uint)keys.Length, cfg);

        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_LowByteDensityTooHigh_DoesNotEmitLowByteBitmapExit()
    {
        EarlyExitConfig cfg = LowByteOnlyConfig();
        int[] keys = Enumerable.Range(0, 129).ToArray();
        DataRanges<int> dataRanges = new DataRanges<int>(1);
        dataRanges.Add(0, 128);

        IEarlyExit[] exits = NumericEarlyExits<int>.GetExits(typeof(ConditionalStructure<,>), keys, dataRanges, 128, 0, (uint)keys.Length, cfg);

        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_BitSetStructure_DoesNotEmitLowByteBitmapExit()
    {
        EarlyExitConfig cfg = LowByteOnlyConfig();
        int[] keys = [0x100, 0x200, 0x300, 0x400, 0x500, 0x600, 0x700, 0x800, 0x900];
        DataRanges<int> dataRanges = new DataRanges<int>(1);
        dataRanges.Add(0x100, 0x900);

        IEarlyExit[] exits = NumericEarlyExits<int>.GetExits(typeof(BitSetStructure<,>), keys, dataRanges, 0x800, 0, (uint)keys.Length, cfg);

        Assert.Empty(exits);
    }

    private static IEarlyExit[] GetExits<T>(List<(T Start, T End)> ranges, ulong range, ulong bitMask, uint itemCount, EarlyExitConfig cfg)
    {
        cfg.DisableEarlyExit(typeof(ValueLowByteBitmapEarlyExit));

        DataRanges<T> dataRanges = new DataRanges<T>(ranges.Count);
        T[] keys = new T[ranges.Count];

        for (int i = 0; i < ranges.Count; i++)
        {
            (T Start, T End) r = ranges[i];
            dataRanges.Add(r.Start, r.End);
            keys[i] = r.Start;
        }

        return NumericEarlyExits<T>.GetExits(typeof(ArrayStructure<,>), keys, dataRanges, range, bitMask, itemCount, cfg);
    }

    private static EarlyExitConfig LowByteOnlyConfig()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableEarlyExit(typeof(ValueBitMaskEarlyExit));
        cfg.DisableEarlyExit(typeof(ValueLessThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueGreaterThanEarlyExit<>));
        cfg.DisableEarlyExit(typeof(ValueBitSetEarlyExit<>));
        cfg.MinRejectionRatio = 0f;
        return cfg;
    }
}