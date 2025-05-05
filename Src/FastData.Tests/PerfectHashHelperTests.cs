using Genbox.FastData.InternalShared;
using static Genbox.FastData.Internal.Helpers.PerfectHashHelper;

namespace Genbox.FastData.Tests;

public class PerfectHashHelperTests
{
    private readonly uint[] _hashCodes;

    public PerfectHashHelperTests()
    {
        string[] words = ["Area", "Army", "Baby", "Back", "Ball", "Band", "Bank", "Base", "Bill", "Body"];
        _hashCodes = words.Select(x => (uint)x.GetHashCode()).ToArray();
    }

    [Fact]
    public void MinimalPerfectHashTest()
    {
        uint seed = Generate(_hashCodes, static (obj, seed) => Mixers.Murmur_32(obj ^ seed), 1);
        Assert.True(Validate(_hashCodes, seed, static (obj, seed) => Mixers.Murmur_32(obj ^ seed), out byte[] _));
    }

    [Fact]
    public void PerfectHashTest()
    {
        uint seed = Generate(_hashCodes, static (obj, seed) => Mixers.Murmur_32(obj ^ seed), length: 64);
        Assert.True(Validate(_hashCodes, seed, static (obj, seed) => Mixers.Murmur_32(obj ^ seed), out byte[] _, 64));
    }
}