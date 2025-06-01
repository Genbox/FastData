using Genbox.FastData.Abstracts;
using Genbox.FastData.Misc;
using Genbox.FastData.StringHash;

namespace Genbox.FastData.Tests;

public class StringHashTests
{
    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void TestVector(IStringHash spec, bool _, ulong vector)
    {
        HashFunc<string> func = spec.GetHashFunction();
        Assert.Equal(vector, func("hello world"));
    }

    public static TheoryData<IStringHash, bool, ulong> GetSpecs() => new TheoryData<IStringHash, bool, ulong>
    {
        { new DefaultStringHash(), true, 16317555765854685474 },
        // { new DefaultStringHash(), false, 15042149576436727275 },

        // { new BruteForceStringHash(new ArraySegment(0, -1, Alignment.Left), ), true, 2374722304133963099) },
        // { new BruteForceStringHash(new ArraySegment(1, 4, Alignment.Left), false, 2837689576666164741) },

        // { new GeneticStringHash(new ArraySegment(0, -1, Alignment.Left), 1, 1, 1, 1), true, 587415029 },
        // { new GeneticStringHash(new ArraySegment(0, -1, Alignment.Left), 2, 1, 2, 1), false, 3487984234 },

        // { new GPerfArrayHash([1]), true, 101 },
        // { new GPerfArrayHash([0, 1]), true, 1765 },
        // { new GPerfArrayHash([0, -1]), true, 1764 },
    };
}