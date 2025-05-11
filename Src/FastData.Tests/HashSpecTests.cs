using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs;
using Genbox.FastData.Specs.Hash;
using Genbox.FastData.Specs.Misc;

namespace Genbox.FastData.Tests;

public class HashSpecTests
{
    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void HashSpecEqualityTest(IStringHash spec, uint vector)
    {
        HashFunc<string> func = spec.GetHashFunction();
        Assert.Equal(vector, func("hello world"));
    }

    public static IEnumerable<object[]> GetSpecs()
    {
        yield return [new BruteForceStringHash(new StringSegment(0, -1, Alignment.Left)), 2256193166];

        yield return [new GeneticStringHash(1, 1, 1, 1, [new StringSegment(0, -1, Alignment.Left)]), 587415029];
        yield return [new GeneticStringHash(2, 1, 2, 1, [new StringSegment(0, -1, Alignment.Left)]), 3487984234];

        yield return [new HeuristicStringHash([1]), 101];
        yield return [new HeuristicStringHash([0, 1]), 1765];
    }
}