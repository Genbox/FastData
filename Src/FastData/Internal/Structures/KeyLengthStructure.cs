using System.Diagnostics;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

public sealed class KeyLengthStructure<TKey, TValue> : IStructure<TKey, TValue, KeyLengthContext<TValue>>
{
    private readonly GeneratorEncoding _encoding;
    private readonly int _maxLength;
    private readonly int _minLength;

    internal KeyLengthStructure(int minLength, int maxLength, GeneratorEncoding encoding)
    {
        _minLength = minLength;
        _maxLength = maxLength;
        _encoding = encoding;
    }

    public KeyLengthContext<TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        Debug.Assert(typeof(TKey) == typeof(string), "KeyLengthStructure requires string keys.");
        Debug.Assert(!keys.IsEmpty, "KeyLengthStructure requires at least one key.");
        Debug.Assert(values.IsEmpty || values.Length == keys.Length, "KeyLengthStructure requires value count to match key count when values are present.");
        Debug.Assert(_minLength >= 0, "KeyLengthStructure requires a non-negative minimum length.");
        Debug.Assert(_maxLength >= _minLength, "KeyLengthStructure requires maxLength to be greater than or equal to minLength.");
        Debug.Assert(_encoding != GeneratorEncoding.Unknown, "KeyLengthStructure requires a known string length encoding.");

        int range = (_maxLength - _minLength) + 1; //+1 because we need a place for zero

        string?[] lengths = new string?[range];
        int[] offsets = values.IsEmpty ? [] : new int[range];
        ReadOnlySpan<TKey> keySpan = keys.Span;

        Func<string, int> getLength = StringHelper.GetLengthFunc(_encoding);
        for (int i = 0; i < keySpan.Length; i++)
        {
            string str = (string)(object)keySpan[i]!;
            int idx = getLength(str) - _minLength;
            Debug.Assert((uint)idx < (uint)lengths.Length, "KeyLengthStructure requires every key length to be within the provided min/max range.");
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