using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Microsoft.Extensions.Logging.Abstractions;
using static Genbox.FastData.Internal.Analysis.KeyAnalyzer;

namespace Genbox.FastData.Tests;

public class PositionLengthAnalyzerTests
{
    [Fact]
    public void GetCandidates_DefaultConfig_ReturnsAllPermutations()
    {
        PositionLengthStringHash[] hashes = GetHashes(new PositionLengthAnalyzerConfig(), ["ax", "bzz", "cyyy"]);

        Assert.Collection(hashes,
            hash => AssertHash(hash, [], true),
            hash => AssertHash(hash, [0], false),
            hash => AssertHash(hash, [0], true),
            hash => AssertHash(hash, [-1], false),
            hash => AssertHash(hash, [-1], true),
            hash => AssertHash(hash, [0, -1], false),
            hash => AssertHash(hash, [0, -1], true));
    }

    [Fact]
    public void GetCandidates_IncludeLengthFalse_OmitsLengthPermutations()
    {
        PositionLengthStringHash[] hashes = GetHashes(new PositionLengthAnalyzerConfig { IncludeLength = false }, ["ax", "bzz", "cyyy"]);

        Assert.Collection(hashes,
            hash => AssertHash(hash, [0], false),
            hash => AssertHash(hash, [-1], false),
            hash => AssertHash(hash, [0, -1], false));
    }

    [Fact]
    public void GetCandidates_IncludeLastCharFalse_OmitsLastCharPermutations()
    {
        PositionLengthStringHash[] hashes = GetHashes(new PositionLengthAnalyzerConfig { IncludeLastChar = false }, ["ax", "bzz", "cyyy"]);

        Assert.Collection(hashes,
            hash => AssertHash(hash, [], true),
            hash => AssertHash(hash, [0], false),
            hash => AssertHash(hash, [0], true));
    }

    [Fact]
    public void GetCandidates_IncludeLengthAndLastCharFalse_ReturnsFirstCharOnly()
    {
        PositionLengthStringHash[] hashes = GetHashes(new PositionLengthAnalyzerConfig { IncludeLength = false, IncludeLastChar = false }, ["ax", "bzz", "cyyy"]);

        PositionLengthStringHash hash = Assert.Single(hashes);
        AssertHash(hash, [0], false);
    }

    [Fact]
    public void GetCandidates_OneUnitKeys_SkipsRedundantLastCharPermutations()
    {
        PositionLengthStringHash[] hashes = GetHashes(new PositionLengthAnalyzerConfig(), ["a", "b", "c"]);

        PositionLengthStringHash hash = Assert.Single(hashes);
        AssertHash(hash, [0], false);
    }

    [Fact]
    public void GetCandidates_LengthOnlyUseful_ReturnsLengthOnlyHash()
    {
        PositionLengthStringHash[] hashes = GetHashes(new PositionLengthAnalyzerConfig(), ["aa", "aaa", "aaaa"]);

        PositionLengthStringHash hash = Assert.Single(hashes);
        AssertHash(hash, [], true);
    }

    [Fact]
    public void GetCandidates_FirstCharNotUseful_ReturnsLastCharHashOnly()
    {
        PositionLengthStringHash[] hashes = GetHashes(new PositionLengthAnalyzerConfig(), ["aa", "ab", "ac"]);

        PositionLengthStringHash hash = Assert.Single(hashes);
        AssertHash(hash, [-1], false);
    }

    [Fact]
    public void GetBestHash_IncludeDefaultOnly_UsesDefaultStringHash()
    {
        string[] data = ["ab", "cd", "ef"];
        StringKeyProperties props = GetStringProperties(data, false, GeneratorEncoding.AsciiBytes);
        StringAnalyzerConfig config = new StringAnalyzerConfig
        {
            BenchmarkIterations = 0,
            PositionLengthAnalyzerConfig = null,
            BruteForceAnalyzerConfig = null,
            GeneticAnalyzerConfig = null,
            GPerfAnalyzerConfig = null
        };

        Candidate candidate = HashBenchmark.GetBestHash(data, props, config, NullLoggerFactory.Instance, GeneratorEncoding.AsciiBytes, true);

        Assert.IsType<DefaultStringHash>(candidate.StringHash);
    }

    private static PositionLengthStringHash[] GetHashes(PositionLengthAnalyzerConfig config, string[] data)
    {
        StringKeyProperties props = GetStringProperties(data, false, GeneratorEncoding.AsciiBytes);
        Simulator sim = new Simulator(data.Length, GeneratorEncoding.AsciiBytes);
        PositionLengthAnalyzer analyzer = new PositionLengthAnalyzer(props, config, sim);

        Assert.True(analyzer.IsAppropriate());

        return analyzer.GetCandidates(data).Select(static candidate => Assert.IsType<PositionLengthStringHash>(candidate.StringHash)).ToArray();
    }

    private static void AssertHash(PositionLengthStringHash hash, int[] positions, bool includeLength)
    {
        Assert.Equal(positions, hash.Positions);
        Assert.Equal(includeLength, hash.IncludeLength);
    }
}