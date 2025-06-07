using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ConditionalStructure<T> : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        //It is inappropriate for large inputs
        if (data.Length > ushort.MaxValue)
        {
            context = null;
            return false;
        }

        context = new ConditionalContext<T>(data);
        return true;
    }
}