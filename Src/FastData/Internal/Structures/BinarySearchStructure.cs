using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class BinarySearchStructure<TKey, TValue> : IStructure<TKey, TValue, BinarySearchContext<TKey, TValue>>
{
    private readonly bool _keysAreSorted;
    private readonly StringComparer? _comparer;

    internal BinarySearchStructure(bool keysAreSorted, StringComparer? comparer)
    {
        _keysAreSorted = keysAreSorted;
        _comparer = comparer;
    }

    public BinarySearchContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (_keysAreSorted)
            return new BinarySearchContext<TKey, TValue>(keys, values);

        TKey[] keysCopy = new TKey[keys.Length];
        keys.CopyTo(keysCopy);

        TValue[] valuesCopy = new TValue[values.Length];
        values.CopyTo(valuesCopy);

        if (typeof(TKey) == typeof(string))
        {
            if (values.IsEmpty)
                Array.Sort(keysCopy, _comparer);
            else
                Array.Sort(keysCopy, valuesCopy, _comparer);
        }
        else
        {
            if (values.IsEmpty)
                Array.Sort(keysCopy);
            else
                Array.Sort(keysCopy, valuesCopy);
        }

        return new BinarySearchContext<TKey, TValue>(keysCopy, valuesCopy);
    }
}