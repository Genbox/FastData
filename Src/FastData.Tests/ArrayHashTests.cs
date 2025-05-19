using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.ArrayHash;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Tests;

public class ArrayHashTests
{
    private readonly byte[] _utf8Bytes = "hello world"u8.ToArray();
    private readonly byte[] _utf16Bytes = Encoding.Unicode.GetBytes("hello world");

    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void StringHashTestVector(IArrayHash spec, bool utf8, ulong vector)
    {
        ArrayHashFunc func = spec.GetHashFunction();
        byte[] bytes = utf8 ? _utf8Bytes : _utf16Bytes;
        Assert.Equal(vector, func(ref bytes[0], bytes.Length));
    }

    public static TheoryData<IArrayHash, bool, ulong> GetSpecs() => new TheoryData<IArrayHash, bool, ulong>
    {
        { new DefaultArrayHash(), true, 16317555765854685474 },
        { new DefaultArrayHash(), false, 15042149576436727275 },

        // { new BruteForceStringHash([new StringSegment(0, -1, Alignment.Left)]), true, 2374722304133963099 },
        // { new BruteForceStringHash([new StringSegment(1, 4, Alignment.Left)]), false, 2837689576666164741 },

        { new GeneticArrayHash(1, 1, 1, 1), true, 587415029 },
        { new GeneticArrayHash(2, 1, 2, 1), false, 3487984234 },

        { new HeuristicArrayHash([1]), true, 101 },
        { new HeuristicArrayHash([0, 1]), true, 1765 },
        { new HeuristicArrayHash([0, -1]), true, 1764 },
    };
}