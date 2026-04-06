using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class BloomFilterContext(ulong[] bitSet) : IContext
{
    public ulong[] BitSet { get; } = bitSet;
}