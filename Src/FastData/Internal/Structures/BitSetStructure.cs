using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

public sealed class BitSetStructure<TKey, TValue> : IStructure<TKey, TValue, BitSetContext<TValue>>
{
    private readonly NumericKeyProperties<TKey> _props;

    internal BitSetStructure(NumericKeyProperties<TKey> props)
    {
        _props = props;
    }

    public BitSetContext<TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (typeof(TKey) == typeof(float) || typeof(TKey) == typeof(double))
            throw new InvalidOperationException("Floating point values are not supported for BitSets");

        ReadOnlySpan<TKey> keySpan = keys.Span;
        ReadOnlySpan<TValue> valueSpan = values.Span;

        int range = (int)(_props.Range + 1);
        ulong[] bitset = new ulong[(range + 63) / 64];
        TValue[]? denseValues = values.IsEmpty ? null : new TValue[range];

        long minKey = _props.ValueConverter(_props.MinKeyValue);

        for (int i = 0; i < keySpan.Length; i++)
        {
            ulong offset = (ulong)(_props.ValueConverter(keySpan[i]) - minKey);
            int word = (int)(offset >> 6);
            bitset[word] |= 1UL << (int)(offset & 63);

            if (denseValues != null)
                denseValues[(int)offset] = valueSpan[i];
        }

        return new BitSetContext<TValue>(bitset, denseValues);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}