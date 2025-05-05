using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure<in T>
{
    bool TryCreate(T[] data, out IContext? context);
}