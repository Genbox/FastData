using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class BitSetStructureTests
{
    [Fact]
    public void HandlesUInt64ValuesAboveInt64Max()
    {
        ReadOnlyMemory<ulong> keys = (ulong[])[9_223_372_036_854_775_804UL, 9_223_372_036_854_775_812UL];
        ReadOnlyMemory<int> values = (int[])[10, 20];

        BitSetStructure<ulong, int> structure = new BitSetStructure<ulong, int>(KeyAnalyzer.GetNumericProperties(keys, false));
        BitSetContext<int> context = structure.Create(keys, values);

        Assert.Equal(0b1_0000_0001UL, context.BitSet[0]);
        Assert.Equal(new[] { 10, 0, 0, 0, 0, 0, 0, 0, 20 }, context.Values.ToArray());
    }
}