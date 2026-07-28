using System.Text;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Tests;

public class StringHashTests
{
    [Theory]
    [MemberData(nameof(GetDefaultHashSpecs))]
    internal void DefaultHashTestVector(StringHashFunc func, bool useUTF16, string value, ulong vector)
    {
        Encoding encoding = useUTF16 ? Encoding.Unicode : Encoding.UTF8;

        byte[] bytes = encoding.GetBytes(value);
        Assert.Equal(vector, func(bytes, bytes.Length));
    }

    [Theory]
    [MemberData(nameof(GetGoXxHash64Specs))]
    internal void XxHash64MatchesGoSum64String(string value, ulong vector)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        StringHashFunc hash = XxHash64StringHash.Instance.GetExpression().Compile();

        Assert.Equal(vector, hash(bytes, bytes.Length));
    }

    public static TheoryData<StringHashFunc, bool, string, ulong> GetDefaultHashSpecs() => new TheoryData<StringHashFunc, bool, string, ulong>
    {
        { DefaultStringHash.UTF16Instance.GetExpression().Compile(), true, "hello world", 16317555765854685474 },
        { DefaultStringHash.UTF8Instance.GetExpression().Compile(), false, "hello world", 16317555765854685474 }
    };

    public static TheoryData<string, ulong> GetGoXxHash64Specs() => new TheoryData<string, ulong>
    {
        { "", 17241709254077376921UL },
        { "a", 15154266338359012955UL },
        { "hello world", 5020219685658847592UL },
        { "0123456789abcdefghijklmnopqrstuv", 13798076798106715874UL },
        { "0123456789abcdefghijklmnopqrstuvwxyzABCD", 1962690110642631947UL },
        { "æther_日本", 12559314775645601297UL }
    };
}