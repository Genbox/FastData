using Genbox.FastData.Generators;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Tests;

public class StringHashTests
{
    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void TestVector(HashFunc<string> func, bool _, ulong vector)
    {
        Assert.Equal(vector, func("hello world"));
    }

    public static TheoryData<HashFunc<string>, bool, ulong> GetSpecs() => new TheoryData<HashFunc<string>, bool, ulong>
    {
        { new DefaultStringHash().GetHashFunction(), true, 16317555765854685474 },
    };
}