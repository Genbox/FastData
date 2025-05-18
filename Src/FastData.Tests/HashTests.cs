using System.Text;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Tests;

public class HashTests
{
    [Theory]
    [InlineData("Hello world", 18344482337671887600)]
    [InlineData("H", 11144410978260493662)]
    public void DJBTest(string input, ulong value)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(input);
        Assert.Equal(value, DJB2Hash.ComputeHash(ref bytes[0], bytes.Length));
    }
}