using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure(StructureConfig config) : IStructure
{
    public bool TryCreate(object[] data, out IContext? context)
    {
        //We make a copy to avoid altering the original data
        object[] copy = new object[data.Length];
        data.CopyTo(copy, 0);

        if (config.DataProperties.DataType == DataType.String)
            Array.Sort(copy, config.GetStringComparer());
        else
            Array.Sort(copy);

        context = new BinarySearchContext(copy);
        return true;
    }
}