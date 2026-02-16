using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class BitSetContext<TValue>(ulong[] bitSet, ReadOnlyMemory<TValue> values) : BitSetContext(bitSet)
{
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

public abstract class BitSetContext(ulong[] bitSet) : IContext
{
    public ulong[] BitSet { get; } = bitSet;
}