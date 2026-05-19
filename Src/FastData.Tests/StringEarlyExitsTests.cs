using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class StringEarlyExitsTests
{
    [Fact]
    public void GetExits_DisabledConfig_ReturnsEmpty()
    {
        string[] keys = ["alpha", "bravo", "charlie"];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.Disabled = true;

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_DisabledForStructure_ReturnsEmpty()
    {
        string[] keys = ["alpha", "bravo", "charlie"];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.DisableForStructure(typeof(ArrayStructure<,>));

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.Empty(exits);
    }

    [Fact]
    public void GetExits_LengthBitmapAndRangeProduced()
    {
        string[] keys = ["a", "abcdefghij"];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;
        cfg.MinItemCount = 0;

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.Contains(exits, static x => x is LengthLessThanEarlyExit { Value: 1 });
        Assert.Contains(exits, static x => x is LengthGreaterThanEarlyExit { Value: 10 });
        Assert.Contains(exits, static x => x is StringLengthRangeEarlyExit { Min: 1, Max: 10 });

        LengthBitmapEarlyExit bitmap = exits.OfType<LengthBitmapEarlyExit>().First();
        Assert.Equal(513UL, bitmap.BitSet);
    }

    [Fact]
    public void GetExits_LengthBitmapNotProducedWhenDensityHigh()
    {
        string[] keys = ["a", "bb", "ccc", "dddd"];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;
        cfg.MinItemCount = 0;

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.DoesNotContain(exits, static x => x is LengthBitmapEarlyExit);
    }

    [Fact]
    public void GetExits_UnitAtBitmapRespectsDensityLimits()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;
        cfg.MinItemCount = 0;

        IEarlyExit[] sparse = GetExits(["alpha", "zulu"], false, cfg);
        Assert.Contains(sparse, static x => x is UnitAtBitmapEarlyExit { Offset: >= 0 });

        IEarlyExit[] dense = GetExits(["apple", "banana"], false, cfg);
        Assert.DoesNotContain(dense, static x => x is UnitAtBitmapEarlyExit { Offset: >= 0 });
    }

    [Fact]
    public void GetExits_IgnoreCaseNonAscii_SkipsUnitExits()
    {
        string[] keys = ["ÆpreOne", "ÆpreTwo", "ÆpreSix"];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;
        cfg.MinItemCount = 0;

        IEarlyExit[] exits = GetExits(keys, true, cfg);

        Assert.Contains(exits, static x => x is LengthGreaterThanEarlyExit);
        Assert.DoesNotContain(exits, static x => x is UnitAtNotEqualEarlyExit or UnitAtLessThanEarlyExit or UnitAtGreaterThanEarlyExit or UnitAtBitmapEarlyExit);
    }

    [Fact]
    public void GetExits_LengthBitmapMapsLength64ToBit63()
    {
        string[] keys = ["a", new string('b', 64)];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;
        cfg.MinItemCount = 0;

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        LengthBitmapEarlyExit bitmap = exits.OfType<LengthBitmapEarlyExit>().Single();
        Assert.Equal((1UL << 0) | (1UL << 63), bitmap.BitSet);
    }

    [Fact]
    public void GetExits_LengthBitmapNotProducedWhenLengthExceeds64()
    {
        string[] keys = ["a", new string('b', 65)];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;
        cfg.MinItemCount = 0;

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.DoesNotContain(exits, static x => x is LengthBitmapEarlyExit);
    }

    private static IEarlyExit[] GetExits(string[] keys, bool ignoreCase, EarlyExitConfig config)
    {
        StringKeyProperties props = KeyAnalyzer.GetStringProperties(keys, ignoreCase, GeneratorEncoding.Utf16CodeUnits);
        return StringEarlyExits.GetExits(typeof(ArrayStructure<,>), props, config, ignoreCase, (uint)keys.Length);
    }
}