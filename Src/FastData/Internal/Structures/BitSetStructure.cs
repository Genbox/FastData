using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BitSetStructure<TKey, TValue>(NumericKeyProperties<TKey> props, KeyType keyType) : IStructure<TKey, TValue, BitSetContext<TKey, TValue>>
{
    public BitSetContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (keyType is KeyType.Single or KeyType.Double)
            throw new InvalidOperationException("Floating point values are not supported for BitSets");

        ReadOnlySpan<TKey> keySpan = keys.Span;
        ReadOnlySpan<TValue> valueSpan = values.Span;

        int range = (int)(props.Range + 1);
        ulong[] bitset = new ulong[(range + 63) / 64];
        TValue[]? denseValues = values.IsEmpty ? null : new TValue[range];

        long minKey = props.ValueConverter(props.MinKeyValue);

        for (int i = 0; i < keySpan.Length; i++)
        {
            ulong offset = (ulong)(props.ValueConverter(keySpan[i]) - minKey);
            int word = (int)(offset >> 6);
            bitset[word] |= 1UL << (int)(offset & 63);

            if (denseValues != null)
                denseValues[(int)offset] = valueSpan[i];
        }

        return new BitSetContext<TKey, TValue>(bitset, denseValues);
    }
}