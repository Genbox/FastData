using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class KeyLengthStructure<TKey, TValue> : IStructure<TKey, TValue, KeyLengthContext<TValue>>
{
    private readonly uint _maxLength;
    private readonly uint _minLength;

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

    public IEnumerable<IEarlyExit> GetMandatoryExits()
    {
        if (_minLength > 0)
            yield return new LengthLessThanEarlyExit(_minLength);

        if (_maxLength < uint.MaxValue)
            yield return new LengthGreaterThanEarlyExit(_maxLength);
    }
}