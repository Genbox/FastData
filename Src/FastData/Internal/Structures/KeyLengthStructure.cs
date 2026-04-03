using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

public sealed class KeyLengthStructure<TKey, TValue> : IStructure<TKey, TValue, KeyLengthContext<TValue>>
{
    private readonly int _maxLength;
    private readonly int _minLength;
    private readonly GeneratorEncoding _encoding;

    internal KeyLengthStructure(int minLength, int maxLength, GeneratorEncoding encoding)
    {
        _minLength = minLength;
        _maxLength = maxLength;
        _encoding = encoding;
    }

    public KeyLengthContext<TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        int range = (_maxLength - _minLength) + 1; //+1 because we need a place for zero

        string?[] lengths = new string?[range];
        int[] offsets = values.IsEmpty ? [] : new int[range];
        ReadOnlySpan<TKey> keySpan = keys.Span;

        Func<string, int> getLength = StringHelper.GetLengthFunc(_encoding);
        for (int i = 0; i < keySpan.Length; i++)
        {
            string str = (string)(object)keySpan[i]!;
            int idx = getLength(str) - _minLength;
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

        if (_maxLength < int.MaxValue)
            yield return new LengthGreaterThanEarlyExit(_maxLength);
    }
}