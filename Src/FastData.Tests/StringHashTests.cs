using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Tests;

public class StringHashTests
{
    private readonly byte[] _utf8Bytes = "hello world"u8.ToArray();
    private readonly byte[] _utf16Bytes = Encoding.Unicode.GetBytes("hello world");

    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void StringHashTestVector(IStringHash spec, bool utf8, ulong vector)
    {
        HashFunc func = spec.GetHashFunction();
        byte[] bytes = utf8 ? _utf8Bytes : _utf16Bytes;
        Assert.Equal(vector, func(ref bytes[0], bytes.Length));
    }

    public static TheoryData<IStringHash, bool, ulong> GetSpecs() => new TheoryData<IStringHash, bool, ulong>
    {
        { new DefaultStringHash(), true, 16317555765854685474 },
        { new DefaultStringHash(), false, 15042149576436727275 },

        // { new BruteForceStringHash([new StringSegment(0, -1, Alignment.Left)]), true, 2374722304133963099 },
        // { new BruteForceStringHash([new StringSegment(1, 4, Alignment.Left)]), false, 2837689576666164741 },

        { new GeneticStringHash(1, 1, 1, 1), true, 587415029 },
        { new GeneticStringHash(2, 1, 2, 1), false, 3487984234 },

        { new HeuristicStringHash([1]), true, 101 },
        { new HeuristicStringHash([0, 1]), true, 1765 },
        { new HeuristicStringHash([0, -1]), true, 1764 },
    };
}