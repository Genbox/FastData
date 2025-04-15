using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ArrayStructure : IStructure
{
    public bool TryCreate(object[] data, out IContext? context)
    {
        context = new ArrayContext(data);
        return true;
    }
}