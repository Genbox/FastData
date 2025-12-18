using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class RangeStructure<TKey, TValue> : IStructure<TKey, TValue, RangeContext<TKey, TValue>>
{
    public RangeContext<TKey, TValue> Create(TKey[] keys, TValue[]? values)
    {
        return new RangeContext<TKey, TValue>();
    }
}
