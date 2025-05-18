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

        for (int len = 1; len < maxLen; len++)
        {
            string[] data = GenerateStrings(len, 1);
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

        byte[] counts = [1, 3, 6, 10, 15, 21, 28, 36, 72, 72];

        for (int i = 1; i <= 10; i++)
        {
            string[] data = GenerateStrings(i, 1);

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

        for (int i = 1; i <= 10; i++)
        {
            string[] data = GenerateStrings(i, 1);

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

        for (int i = 8; i <= 32; i++)
        {
            string[] data = GenerateStrings(i, 1);

            StringProperties props = DataAnalyzer.GetStringProperties(data);
            Assert.True(gen.IsAppropriate(props));
            Assert.NotEmpty(gen.Generate(props));
        }
    }

    private static string[] GenerateStrings(int len, int count)
    {
        Random rng = new Random(42);
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