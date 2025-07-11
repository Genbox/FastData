using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ArrayStructure<T> : IStructure<T, ArrayContext<T>>
{
    public ArrayContext<T> Create(T[] data)
    {
        return new ArrayContext<T>(data);
    }
}