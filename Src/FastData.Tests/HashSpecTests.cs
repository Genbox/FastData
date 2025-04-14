using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Analyzers.BruteForce;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic;
using Genbox.FastData.Internal.Analysis.Analyzers.Heuristics;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Tests;

public class HashSpecTests
{
    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void HashSpecEqualityTest(IHashSpec spec, uint vector)
    {
        HashFunc func = spec.GetHashFunction();
        Assert.Equal(vector, func("hello world"));
    }

    public static IEnumerable<object[]> GetSpecs()
    {
        yield return [new BruteForceHashSpec(HashFunction.DJB2Hash, [new StringSegment(0, -1, Alignment.Left)]), 1853903583];
        yield return [new BruteForceHashSpec(HashFunction.XxHash, [new StringSegment(0, -1, Alignment.Left)]), 1713611331];

        yield return [new GeneticHashSpec(1, 1, 1, 1, [new StringSegment(0, -1, Alignment.Left)]), 2138145203];
        yield return [new GeneticHashSpec(2, 1, 2, 1, [new StringSegment(0, -1, Alignment.Left)]), 401880771];

        yield return [new HeuristicHashSpec([1]), 101];
        yield return [new HeuristicHashSpec([0, 1]), 104];
    }
}