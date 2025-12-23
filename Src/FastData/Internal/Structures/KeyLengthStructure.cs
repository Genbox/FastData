using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

internal sealed class KeyLengthStructure<TKey, TValue>(StringKeyProperties props) : IStructure<TKey, TValue, KeyLengthContext<TValue>>
{
    public KeyLengthContext<TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (typeof(TKey) != typeof(string))
            throw new InvalidCastException("This structure only works on strings");

        if (!props.LengthData.Unique)
            throw new InvalidOperationException("We can only use this structure when all lengths are unique");

        uint minLen = props.LengthData.LengthMap.Min;
        uint maxLen = props.LengthData.LengthMap.Max;
        int range = (int)(maxLen - minLen + 1); //+1 because we need a place for zero

        string?[] lengths = new string?[range];
        int[] offsets = values.IsEmpty ? [] : new int[range];
        ReadOnlySpan<TKey> keySpan = keys.Span;

        for (int i = 0; i < keySpan.Length; i++)
        {
            string str = (string)(object)keySpan[i]!;
            int idx = str.Length - (int)minLen;
            lengths[idx] = str;

            if (!values.IsEmpty)
                offsets[idx] = i;
        }

        return new KeyLengthContext<TValue>(lengths, minLen, values, offsets);
    }
}