using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure<TKey, TValue>(KeyType keyType, StringComparison comparison) : IStructure<TKey, TValue, BinarySearchContext<TKey, TValue>>
{
    public BinarySearchContext<TKey, TValue> Create(TKey[] keys, TValue[]? values)
    {
        TKey[] keysCopy = new TKey[keys.Length];
        keys.CopyTo(keysCopy, 0);

        TValue[]? valuesCopy;

        if (values == null)
            valuesCopy = null;
        else
        {
            valuesCopy = new TValue[values.Length];
            values.CopyTo(valuesCopy, 0);
        }

        if (keyType == KeyType.String)
        {
            if (valuesCopy != null)
                Array.Sort(keysCopy, valuesCopy, StringHelper.GetStringComparer(comparison));
            else
                Array.Sort(keysCopy, StringHelper.GetStringComparer(comparison));
        }
        else
        {
            if (valuesCopy != null)
                Array.Sort(keysCopy, valuesCopy);
            else
                Array.Sort(keysCopy);
        }

        return new BinarySearchContext<TKey, TValue>(keysCopy, valuesCopy);
    }
}