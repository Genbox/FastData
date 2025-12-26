using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure<TKey, TValue>(KeyType keyType, bool ignoreCase) : IStructure<TKey, TValue, BinarySearchContext<TKey, TValue>>
{
    public BinarySearchContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        TKey[] keysCopy = new TKey[keys.Length];
        keys.Span.CopyTo(keysCopy);

        TValue[] valuesCopy = Array.Empty<TValue>();

        if (!values.IsEmpty)
        {
            valuesCopy = new TValue[values.Length];
            values.Span.CopyTo(valuesCopy);
        }

        if (keyType == KeyType.String)
        {
            if (!values.IsEmpty)
                Array.Sort(keysCopy, valuesCopy, StringHelper.GetStringComparer(ignoreCase));
            else
                Array.Sort(keysCopy, StringHelper.GetStringComparer(ignoreCase));
        }
        else
        {
            if (!values.IsEmpty)
                Array.Sort(keysCopy, valuesCopy);
            else
                Array.Sort(keysCopy);
        }

        return new BinarySearchContext<TKey, TValue>(keysCopy, valuesCopy);
    }
}