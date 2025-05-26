using Genbox.FastData.Internal.Analysis.Misc;

namespace Genbox.FastData.Tests;

public class MinHeapTests
{
    [Theory]
    [InlineData(new double[] { 1, 1, 1 }, new double[] { 1 })] // If capacity is 1, we should merge all values
    [InlineData(new double[] { 1, 1, 2, 2 }, new double[] { 2, 2 })] // We keep the highest values inserted
    [InlineData(new double[] { 1, 2, 3, 1 }, new double[] { 1, 2, 3 })] // Tests if we add 4 elements and the last one is smaller, we should keep the first 3
    [InlineData(new double[] { 1, 2, 3, 4 }, new double[] { 2, 4, 3 })] // If we insert a larger item, it should persist
    public void GenericTest(double[] input, double[] expected)
    {
        MinHeap<bool> buffer = new MinHeap<bool>(expected.Length);

        foreach (double value in input)
            buffer.Add(value, true);

        Assert.Equal(expected, buffer.Keys);
    }
}