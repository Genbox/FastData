using System.Diagnostics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Extensions;
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
        Debug.Assert(!keys.IsEmpty, "BitSetStructure requires at least one key.");
        Debug.Assert(values.IsEmpty || values.Length == keys.Length, "BitSetStructure requires value count to match key count when values are present.");
        Debug.Assert(_props.Range < int.MaxValue, "BitSetStructure requires a range that fits in an int-backed dense table.");

        if (typeof(TKey) == typeof(float) || typeof(TKey) == typeof(double))
            throw new InvalidOperationException("Floating point values are not supported for BitSets");

        ReadOnlySpan<TKey> keySpan = keys.Span;
        ReadOnlySpan<TValue> valueSpan = values.Span;

        int range = (int)(_props.Range + 1);
        ulong[] bitset = new ulong[(range + 63) / 64];
        TValue[]? denseValues = values.IsEmpty ? null : new TValue[range];

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        if (typeCode.IsUnsigned())
        {
            Func<TKey, ulong> conv = typeCode.GetUnsignedValueConverter<TKey>();
            ulong minKey = conv(_props.DataRanges.Min);

            for (int i = 0; i < keySpan.Length; i++)
                SetEntry(conv(keySpan[i]) - minKey, denseValues == null ? default! : valueSpan[i]);
        }
        else
        {
            Func<TKey, long> conv = typeCode.GetSignedValueConverter<TKey>();
            long minKey = conv(_props.DataRanges.Min);

            for (int i = 0; i < keySpan.Length; i++)
                SetEntry((ulong)(conv(keySpan[i]) - minKey), denseValues == null ? default! : valueSpan[i]);
        }

        return new BitSetContext<TValue>(bitset, denseValues);

        void SetEntry(ulong offset, TValue value)
        {
            Debug.Assert(offset <= _props.Range, "BitSetStructure requires every key to be within the analyzed numeric range.");
            int word = (int)(offset >> 6);
            bitset[word] |= 1UL << (int)(offset & 63);

            if (denseValues != null)
                denseValues[(int)offset] = value;
        }
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}