using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchInterpolationStructure<TKey, TValue>(bool keysAreSorted = false) : IStructure<TKey, TValue, BinarySearchInterpolationContext<TKey, TValue>>
{
    public BinarySearchInterpolationContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (keysAreSorted)
            return new BinarySearchInterpolationContext<TKey, TValue>(keys, values);

        TKey[] keysCopy = new TKey[keys.Length];
        keys.CopyTo(keysCopy);

        TValue[] valuesCopy = new TValue[values.Length];
        values.CopyTo(valuesCopy);

        if (values.IsEmpty)
            Array.Sort(keysCopy);
        else
            Array.Sort(keysCopy, valuesCopy);

        return new BinarySearchInterpolationContext<TKey, TValue>(keysCopy, valuesCopy);
    }
}