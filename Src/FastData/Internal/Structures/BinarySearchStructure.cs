using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure<T>(DataType dataType, StringComparison comparison) : IStructure<T, BinarySearchContext<T>>
{
    public BinarySearchContext<T> Create(ref ReadOnlySpan<T> data)
    {
        T[] copy = new T[data.Length];
        data.CopyTo(copy);

        if (dataType == DataType.String)
            Array.Sort(copy, StringHelper.GetStringComparer(comparison));
        else
            Array.Sort(copy);

        data = copy;
        return new BinarySearchContext<T>();
    }
}