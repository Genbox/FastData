using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure<T>(DataType dataType, StringComparison comparison) : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        //We make a copy to avoid altering the original data
        T[] copy = new T[data.Length];
        data.CopyTo(copy, 0);

        if (dataType == DataType.String)
            Array.Sort(copy, StringHelper.GetStringComparer(comparison));
        else
            Array.Sort(copy);

        context = new BinarySearchContext<T>(copy);
        return true;
    }
}