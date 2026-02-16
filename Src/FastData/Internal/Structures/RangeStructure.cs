using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

internal sealed class RangeStructure<TKey, TValue>(NumericKeyProperties<TKey> props) : IStructure<TKey, TValue, RangeContext<TKey>>
{
    public RangeContext<TKey> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        return new RangeContext<TKey>(props.MinKeyValue, props.MaxKeyValue);
    }
}