using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class RangeStructure<TKey, TValue> : IStructure<TKey, TValue, RangeContext<TKey>>
{
    private readonly TKey _minValue;
    private readonly TKey _maxValue;

    internal RangeStructure(TKey minValue, TKey maxValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;
    }

    public RangeContext<TKey> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) => new RangeContext<TKey>(_minValue, _maxValue);
}