using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class RangeStructure<TKey, TValue> : IStructure<TKey, TValue, RangeContext<TKey, TValue>>
{
    public RangeContext<TKey, TValue> Create(TKey[] keys, TValue[]? values)
    {
        if (values == null)
            return new RangeContext<TKey, TValue>(values);

        Array.Sort(keys, values); // Sort in-place because the structure is already chosen and now have ownership
        return new RangeContext<TKey, TValue>(values);
    }
}