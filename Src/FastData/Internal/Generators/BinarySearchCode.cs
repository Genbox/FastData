using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class BinarySearchCode : IStructure
{
    public IContext Create(object[] data)
    {
        Array.Sort(data, StringComparer.Ordinal);
        return new BinarySearchContext(data);
    }
}