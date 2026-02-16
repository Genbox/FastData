using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public class BloomFilterContext(ulong[] bitSet) : IContext
{
    public ulong[] BitSet { get; } = bitSet;
}