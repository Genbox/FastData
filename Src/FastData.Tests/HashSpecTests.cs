using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Techniques.BruteForce;
using Genbox.FastData.Internal.Analysis.Techniques.Heuristics;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Tests;

public class HashSpecTests
{
    /// <summary>Tests if GetFunction() and GetSource() returns the same values for the same inputs</summary>
    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void HashSpecEqualityTest(IHashSpec spec, uint vector)
    {
        Func<string, uint> func = spec.GetFunction();
        Assert.Equal(vector, func("hello world"));

        //The source code must give the same result
        string source = spec.GetSource();
        Func<string, uint> func2 = CompilationHelper.GetDelegate<Func<string, uint>>(source, false);
        Assert.Equal(vector, func2("hello world"));
    }

    public static IEnumerable<object[]> GetSpecs()
    {
        yield return [new BruteForceHashSpec(HashFunction.DJB2Hash, [new StringSegment(0, -1, Alignment.Left)]), 1853903583];
        yield return [new BruteForceHashSpec(HashFunction.XxHash, [new StringSegment(0, -1, Alignment.Left)]), 1713611331];

        yield return [new GeneticHashSpec(1, 1, 1, 1, [new StringSegment(0, -1, Alignment.Left)]), 2138145203];
        yield return [new GeneticHashSpec(2, 1, 2, 1, [new StringSegment(0, -1, Alignment.Left)]), 401880771];
    }
}