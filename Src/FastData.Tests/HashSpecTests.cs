using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs;
using Genbox.FastData.Specs.Hash;

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
        yield return [new BruteForceHashSpec(HashFunction.DJB2Hash, [new StringSegment(0, -1, Alignment.Left)]), 2256193166];
        yield return [new BruteForceHashSpec(HashFunction.XxHash, [new StringSegment(0, -1, Alignment.Left)]), 1713611331];

        yield return [new GeneticHashSpec(1, 1, 1, 1, [new StringSegment(0, -1, Alignment.Left)]), 587415029];
        yield return [new GeneticHashSpec(2, 1, 2, 1, [new StringSegment(0, -1, Alignment.Left)]), 3487984234];

        yield return [new HeuristicHashSpec([1]), 101];
        yield return [new HeuristicHashSpec([0, 1]), 1765];
    }
}