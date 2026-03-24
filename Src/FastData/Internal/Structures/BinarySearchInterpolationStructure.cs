using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class BinarySearchInterpolationStructure<TKey, TValue> : IStructure<TKey, TValue, BinarySearchInterpolationContext<TKey, TValue>>
{
    private readonly bool _keysAreSorted;

    internal BinarySearchInterpolationStructure(bool keysAreSorted)
    {
        _keysAreSorted = keysAreSorted;
    }

    public BinarySearchInterpolationContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (_keysAreSorted)
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

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}