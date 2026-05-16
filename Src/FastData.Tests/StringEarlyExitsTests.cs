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

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.DoesNotContain(exits, static x => x is LengthBitmapEarlyExit);
    }

    [Fact]
    public void GetExits_CharFirstBitmapRespectsDensityLimits()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;

        IEarlyExit[] sparse = GetExits(["alpha", "zulu"], false, cfg);
        Assert.Contains(sparse, static x => x is CharFirstBitmapEarlyExit);

        IEarlyExit[] dense = GetExits(["apple", "banana"], false, cfg);
        Assert.DoesNotContain(dense, static x => x is CharFirstBitmapEarlyExit);
    }

    [Fact]
    public void GetExits_PrefixSuffixProducedWhenTrimmingEnabled()
    {
        string[] keys = ["preOneSuf", "preTwoSuf", "preSixSuf"];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0f;

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.Contains(exits, static x => x is StringPrefixEarlyExit { Prefix: "pre" });
        Assert.Contains(exits, static x => x is StringSuffixEarlyExit { Suffix: "Suf" });
    }

    [Fact]
    public void GetExits_RejectionBelowThreshold_DiscardsShortAffix()
    {
        string[] keys = ["abshort", "abmediumlen", "abveryverylong"];
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;
        cfg.MinRejectionRatio = 0.5f;

        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.DoesNotContain(exits, static x => x is StringPrefixEarlyExit { Prefix: "ab" });
    }

    [Fact]
    public void GetExits_ObservedRangeBaseline_KeepsShortAffix()
    {
        EarlyExitConfig cfg = EarlyExitConfig.Default;
        cfg.MaxCandidates = 50;

        // Prefix is large relative to the observed length span, so the observed ratio keeps it.
        string[] keys = ["abaaaaaa", "abbbbbbb", "abccccccc"];
        IEarlyExit[] exits = GetExits(keys, false, cfg);

        Assert.Contains(exits, static x => x is StringPrefixEarlyExit { Prefix: "ab" });

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(keys, false, GeneratorEncoding.Utf16CodeUnits);
        int minLength = props.LengthData.LengthRanges.Min;
        int maxLength = props.LengthData.LengthRanges.Max;
        double lengthSpan = (maxLength - minLength) + 1d;

        // Observed-length ratio keeps this prefix.
        Assert.True("ab".Length / lengthSpan >= 0.5d);
    }

    private static IEarlyExit[] GetExits(string[] keys, bool ignoreCase, EarlyExitConfig config)
    {
        StringKeyProperties props = KeyAnalyzer.GetStringProperties(keys, ignoreCase, GeneratorEncoding.Utf16CodeUnits);
        return StringEarlyExits.GetExits(typeof(ArrayStructure<,>), props, config, ignoreCase);
    }
}