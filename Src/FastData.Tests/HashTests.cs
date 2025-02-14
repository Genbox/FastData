using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;

namespace Genbox.FastData.Tests;

public class HashTests
{
    [Theory]
    [InlineData("Hello world", 1853904575u)]
    [InlineData("H", 757113168u)]
    public void DJBTest(string input, uint value)
    {
        Assert.Equal(value, DJB2Hash.ComputeHash(ref MemoryMarshal.GetReference(input.AsSpan()), input.Length));
    }
}