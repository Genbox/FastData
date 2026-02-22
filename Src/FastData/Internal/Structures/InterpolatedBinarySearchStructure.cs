using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class InterpolatedBinarySearchStructure<TKey, TValue>(bool keysAreSorted = false) : IStructure<TKey, TValue, InterpolatedBinarySearchContext<TKey, TValue>>
{
    public InterpolatedBinarySearchContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (keysAreSorted)
            return new InterpolatedBinarySearchContext<TKey, TValue>(keys, values);

        TKey[] keysCopy = new TKey[keys.Length];
        keys.CopyTo(keysCopy);

        TValue[] valuesCopy = new TValue[values.Length];
        values.CopyTo(valuesCopy);

        if (values.IsEmpty)
            Array.Sort(keysCopy);
        else
            Array.Sort(keysCopy, valuesCopy);

        return new InterpolatedBinarySearchContext<TKey, TValue>(keysCopy, valuesCopy);
    }
}