using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class InterpolatedBinarySearchStructure<TKey, TValue> : IStructure<TKey, TValue, InterpolatedBinarySearchContext<TKey, TValue>>
{
    public InterpolatedBinarySearchContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        TKey[] keysCopy = new TKey[keys.Length];
        keys.Span.CopyTo(keysCopy);

        TValue[] valuesCopy = [];

        if (!values.IsEmpty)
        {
            valuesCopy = new TValue[values.Length];
            values.Span.CopyTo(valuesCopy);
        }

        if (!values.IsEmpty)
            Array.Sort(keysCopy, valuesCopy);
        else
            Array.Sort(keysCopy);

        return new InterpolatedBinarySearchContext<TKey, TValue>(keysCopy, valuesCopy);
    }
}
