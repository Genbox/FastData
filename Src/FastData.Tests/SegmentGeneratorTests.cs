using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.Segments;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.InternalShared;
using Genbox.FastData.Specs.Misc;

namespace Genbox.FastData.Tests;

[SuppressMessage("Usage", "xUnit1016:MemberData must reference a public member")]
public class SegmentGeneratorTests(ITestOutputHelper o)
{
    [Theory, MemberData(nameof(GetGenerators))]
    internal void CoverageTest(ISegmentGenerator generator, int maxLen)
    {
        //Tests if BruteForceGenerator, EdgeGramGenerator and OffsetGenerator covers the entire string for lengths [1..max]
        Random rng = new Random(42);

        for (int len = 1; len < maxLen; len++)
        {
            string[] data = GenerateStrings(rng, len, 1);
            StringProperties props = DataAnalyzer.GetStringProperties(data);
            int[] coverage = new int[len]; // Track how many times each index is covered

            foreach (StringSegment segment in generator.Generate(props))
            {
                SegmentHelper.ConvertToOffsets(data[0].Length, in segment, out int start, out int end);

                for (int i = start; i < end; i++)
                {
                    coverage[i]++;
                }

                o.WriteLine(segment.Alignment.ToString() + '\t' + SegmentHelper.InsertSegmentBounds(data[0], segment));
            }

            Assert.All(coverage, i => Assert.NotEqual(0, i));
        }
    }

    [Fact]
    public void BruteForceGeneratorTest()
    {
        // The generator should provide n*n number of results for strings up to length 8
        BruteForceGenerator gen = new BruteForceGenerator();
        Random rng = new Random(42);

        byte[] counts = [2, 6, 12, 20, 30, 42, 56, 72, 72, 72];

        for (int i = 1; i <= 10; i++)
        {
            string[] data = GenerateStrings(rng, i, 1);

            StringProperties props = DataAnalyzer.GetStringProperties(data);
            Assert.True(gen.IsAppropriate(props));
            Assert.Equal(counts[i - 1], gen.Generate(props).Count());
        }
    }

    [Fact]
    public void EdgeGramGeneratorTest()
    {
        // The generator should provide n*n number of results for strings up to length 8
        EdgeGramGenerator gen = new EdgeGramGenerator();
        Random rng = new Random(42);

        for (int i = 1; i <= 10; i++)
        {
            string[] data = GenerateStrings(rng, i, 1);

            StringProperties props = DataAnalyzer.GetStringProperties(data);
            Assert.True(gen.IsAppropriate(props));

            int max = Math.Min(i * 2, 16);
            Assert.Equal(max, gen.Generate(props).Count());
        }
    }

    [Fact]
    public void DeltaGeneratorTest()
    {
        // The generator should provide n*n number of results for strings up to length 8
        DeltaGenerator gen = new DeltaGenerator();
        Random rng = new Random(42);

        for (int i = 8; i <= 32; i++)
        {
            string[] data = GenerateStrings(rng, i, 2);

            StringProperties props = DataAnalyzer.GetStringProperties(data);
            Assert.True(gen.IsAppropriate(props));
            Assert.NotEmpty(gen.Generate(props));
        }
    }

    [Theory]
    [InlineData(new[] { "aax9halbb", "aarexf1bb" }, 2, 5)] //Test same length
    [InlineData(new[] { "aax9halbb", "aarexf1" }, 2, 5)] //Test diff length
    [InlineData(new[] { "aa", "bb" }, 0, 2)] //Test diff length with identical chars
    [InlineData(new[] { "aa", "bbbbbbbbbbbb" }, 0, 2)] //Test larger diff length with identical chars
    [InlineData(new[] { "aaxbb", "aanbb" }, 2, 1)] //Test single char difference
    public void DeltaGeneratorPatternTest(string[] input, uint offset, int length)
    {
        StringProperties props = DataAnalyzer.GetStringProperties(input);

        DeltaGenerator gen = new DeltaGenerator();
        Assert.True(gen.IsAppropriate(props)); //We allow delta always

        StringSegment expected = new StringSegment(offset, length, Alignment.Left);
        StringSegment[] res = gen.Generate(props).ToArray();

        foreach (StringSegment segment in res)
        {
            o.WriteLine($"{segment}. res: {string.Join(",", input.Select(x => SegmentHelper.InsertSegmentBounds(x, segment)))}");
        }

        Assert.Equal(res[0], expected);
    }

    [Theory]
    [InlineData((object)new[] { "aa", "aaaaaaaaaaaaaa" })] //We don't support inputs where characters don't differ
    public void DeltaGeneratorFailureTest(string[] input)
    {
        StringProperties props = DataAnalyzer.GetStringProperties(input);

        DeltaGenerator gen = new DeltaGenerator();
        Assert.Empty(gen.Generate(props));
    }

    private static string[] GenerateStrings(Random rng, int len, int count)
    {
        string[] res = new string[count];

        for (int i = 0; i < count; i++)
        {
            res[i] = TestHelper.GenerateRandomString(rng, len);
        }

        return res;
    }

    internal static TheoryData<ISegmentGenerator, int> GetGenerators() => new TheoryData<ISegmentGenerator, int>
    {
        { new BruteForceGenerator(), BruteForceGenerator.MaxLength },
        { new EdgeGramGenerator(), EdgeGramGenerator.MaxLength },
        { new OffsetGenerator(), 8 } // There is no maxlength, but we test up to 8
    };
}