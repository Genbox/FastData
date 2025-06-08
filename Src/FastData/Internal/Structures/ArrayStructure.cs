using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ArrayStructure<T> : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        context = new ArrayContext<T>(data);
        return true;
    }
}