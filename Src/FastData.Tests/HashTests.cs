using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Tests;

public class HashTests
{
    [Theory]
    [InlineData("Hello world", 1638393291)]
    [InlineData("H", 757113168u)]
    public void DJBTest(string input, uint value)
    {
        Assert.Equal(value, DJB2Hash.ComputeHash(ref MemoryMarshal.GetReference(input.AsSpan()), input.Length));
    }
}