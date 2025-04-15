using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class SingleValueStructure : IStructure
{
    public bool TryCreate(object[] data, out IContext? context)
    {
        if (data.Length != 1)
        {
            context = null;
            return false;
        }

        context = new SingleValueContext(data[0]);
        return true;
    }
}