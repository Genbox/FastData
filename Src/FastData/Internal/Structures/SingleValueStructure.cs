using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class SingleValueStructure<T> : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        if (data.Length != 1)
        {
            context = null;
            return false;
        }

        context = new SingleValueContext<T>(data[0]);
        return true;
    }
}