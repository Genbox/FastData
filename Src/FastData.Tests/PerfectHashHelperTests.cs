using Genbox.FastData.InternalShared;
using static Genbox.FastData.Internal.Helpers.PerfectHashHelper;

namespace Genbox.FastData.Tests;

public class PerfectHashHelperTests
{
    private readonly uint[] _hashCodes;

    public PerfectHashHelperTests()
    {
        string[] words = ["Area", "Army", "Baby", "Back", "Ball", "Band", "Bank", "Base", "Bill", "Body"];
        _hashCodes = words.Select(x => unchecked((uint)x.GetHashCode())).ToArray();
    }

    [Fact]
    public void MinimalPerfectHashTest()
    {
        uint seed = Generate(_hashCodes, static (obj, seed) => Mixers.Murmur_32(obj ^ seed), 100_000);
        Assert.NotEqual(0u, seed);
    }

    [Fact]
    public void PerfectHashTest()
    {
        uint seed = Generate(_hashCodes, static (obj, seed) => Mixers.Murmur_32(obj ^ seed), 100_000, 64);
        Assert.NotEqual(0u, seed);
    }
}