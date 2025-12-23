using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

internal sealed class RangeStructure<TKey, TValue>(KeyProperties<TKey> props) : IStructure<TKey, TValue, RangeContext<TKey, TValue>>
{
    public RangeContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        return new RangeContext<TKey, TValue>(props.MinKeyValue, props.MaxKeyValue);
    }
}