using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure<TKey, TValue>(bool ignoreCase, bool keysAreSorted = false) : IStructure<TKey, TValue, BinarySearchContext<TKey, TValue>>
{
    public BinarySearchContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (keysAreSorted)
            return new BinarySearchContext<TKey, TValue>(keys, values);

        TKey[] keysCopy = new TKey[keys.Length];
        keys.CopyTo(keysCopy);

        TValue[] valuesCopy = new TValue[values.Length];
        values.CopyTo(valuesCopy);

        if (typeof(TKey) == typeof(string))
        {
            if (values.IsEmpty)
                Array.Sort(keysCopy, StringHelper.GetStringComparer(ignoreCase));
            else
                Array.Sort(keysCopy, valuesCopy, StringHelper.GetStringComparer(ignoreCase));
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