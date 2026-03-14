using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class KeyLengthStructure<TKey, TValue> : IStructure<TKey, TValue, KeyLengthContext<TValue>>
{
    private readonly uint _minLength;
    private readonly uint _maxLength;

    internal KeyLengthStructure(uint minLength, uint maxLength)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    public KeyLengthContext<TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        int range = (int)((_maxLength - _minLength) + 1); //+1 because we need a place for zero

        string?[] lengths = new string?[range];
        int[] offsets = values.IsEmpty ? [] : new int[range];
        ReadOnlySpan<TKey> keySpan = keys.Span;

        for (int i = 0; i < keySpan.Length; i++)
        {
            string str = (string)(object)keySpan[i]!;
            int idx = str.Length - (int)_minLength;
            lengths[idx] = str;

            if (!values.IsEmpty)
                offsets[idx] = i;
        }

        return new KeyLengthContext<TValue>(lengths, _minLength, values, offsets);
    }
}