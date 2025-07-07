using System.Text;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Tests;

public class StringHashTests
{
    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void TestVector(StringHashFunc func, bool useUTF16, ulong vector)
    {
        Encoding encoding = useUTF16 ? Encoding.Unicode : Encoding.UTF8;

        byte[] bytes = encoding.GetBytes("hello world");
        Assert.Equal(vector, func(bytes, bytes.Length));
    }

    public static TheoryData<StringHashFunc, bool, ulong> GetSpecs() => new TheoryData<StringHashFunc, bool, ulong>
    {
        { DefaultStringHash.UTF16Instance.GetExpression().Compile(), true, 16317555765854685474 },
        { DefaultStringHash.UTF8Instance.GetExpression().Compile(), false, 16317555765854685474 },
    };
}