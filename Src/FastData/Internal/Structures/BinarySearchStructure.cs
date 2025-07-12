using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure<TKey, TValue>(DataType dataType, StringComparison comparison) : IStructure<TKey, TValue, BinarySearchContext<TKey, TValue>>
{
    public BinarySearchContext<TKey, TValue> Create(TKey[] data, ValueSpec<TValue>? valueSpec)
    {
        TKey[] copy = new TKey[data.Length];
        data.CopyTo(copy, 0);

        if (dataType == DataType.String)
            Array.Sort(copy, StringHelper.GetStringComparer(comparison));
        else
            Array.Sort(copy);

        return new BinarySearchContext<TKey, TValue>(copy, valueSpec);
    }
}